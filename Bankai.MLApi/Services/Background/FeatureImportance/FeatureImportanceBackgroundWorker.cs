using System.Collections.Immutable;
using System.Threading.Tasks.Dataflow;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.FeatureImportance.Data;
using Bankai.MLApi.Services.ModelManagement;
using Microsoft.ML.Data;

namespace Bankai.MLApi.Services.Background.FeatureImportance;

public class FeatureImportanceBackgroundWorker : IFeatureImportanceBackgroundService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<FeatureImportanceBackgroundWorker> _logger;
    private readonly ActionBlock<GetFeatureImportanceData> _featureImportanceStream;
    private readonly IServiceProvider _serviceProvider;

    public FeatureImportanceBackgroundWorker(MLContext mlContext, ILogger<FeatureImportanceBackgroundWorker> logger, IServiceProvider serviceProvider)
    {
        _mlContext = mlContext;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _featureImportanceStream =
            new ActionBlock<GetFeatureImportanceData>(GetFeatureImportanceAsync, new() { MaxDegreeOfParallelism = 4 });
    }

    public async Task SendAsync(GetFeatureImportanceData data) => await _featureImportanceStream.SendAsync(data);

    public Task<Result<List<FeatureImportanceMetric>>> GetFeatureImportanceAsync(GetFeatureImportanceData data) =>
        Result.Success(data)
            .Bind(d => d.Model.Prediction switch
                {
                    PredictionType.Regression => GetRegressionFeatureImportance(data),
                    PredictionType.BinaryClassification => GetBinaryClassificationFeatureImportance(data),
                    PredictionType.MulticlassClassification => GetMulticlassClassificationFeatureImportance(
                        data),
                    _ => throw new ArgumentException($"Unsupported prediction type {d.Model.Prediction}")
                }
            )
            .TapError(async e =>
            {
                _logger.LogError("Error when trying to get feature importance of model ({Id}): {Exception}", data.Model.Id, e);
                using var scope = _serviceProvider.CreateScope();
                var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
                if (data.OnError != null) await data.OnError(e, modelManagementService);
            })
            .TapTry(async l =>
            {
                using var scope = _serviceProvider.CreateScope();
                var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
                if (data.OnSaveResult != null) await data.OnSaveResult.Invoke(l, modelManagementService);
            })
            .Tap(_ => _logger.LogInformation("Successfully saved feature importance of model({Id})", data.Model.Id))
            .TapError(e =>
                _logger.LogError("Saving feature importance of ({ModelId}) error: {Exception}", data.Model.Id, e))
            .Map(f => f);

    private Result<List<FeatureImportanceMetric>> GetRegressionFeatureImportance(GetFeatureImportanceData data) =>
        Result.Success(data)
            .Map(d => (data: d, loadedData: TransformData(d.Model.Data, d.LoadedDatasetData).Value))
            .Map(t => _mlContext.Regression
                .PermutationFeatureImportance(t.loadedData.transformer, t.loadedData.data,
                    t.data.Model.Features.First(f => f.IsTarget).Name, permutationCount: t.data.PermutationCount)
                .OrderBy(column => column.Value.RootMeanSquaredError.Mean).ToImmutableDictionary())
            .TapError(e =>
                _logger.LogInformation("Error getting feature importance for model {id}: {Exception}", data.Model.Id,
                    e))
            .Tap(_ => _logger.LogInformation("Successfully get feature importance for model {id}", data.Model.Id))
            .Bind(ParseMetrics);

    private Result<List<FeatureImportanceMetric>> GetBinaryClassificationFeatureImportance(GetFeatureImportanceData data) =>
        Result.Success(data)
            .Map(d => (data: d, loadedData: TransformData(d.Model.Data, d.LoadedDatasetData).Value))
            .Map(t => _mlContext.BinaryClassification
                .PermutationFeatureImportanceNonCalibrated(
                    (ISingleFeaturePredictionTransformer<object>)
                    (t.loadedData.transformer as TransformerChain<ITransformer>)!.LastTransformer,
                    t.loadedData.data,
                    t.data.Model.Features.First(f => f.IsTarget).Name,
                    permutationCount: t.data.PermutationCount))
            .TapError(e =>
                _logger.LogInformation("Error getting feature importance for model {id}: {Exception}", data.Model.Id,
                    e))
            .Tap(_ => _logger.LogInformation("Successfully get feature importance for model {id}", data.Model.Id))
            .Bind(ParseMetrics);

    private Result<List<FeatureImportanceMetric>> GetMulticlassClassificationFeatureImportance(GetFeatureImportanceData data) =>
        Result.Success(data)
            .Map(d => (data: d, loadedData: TransformData(d.Model.Data, d.LoadedDatasetData).Value))
            .Map(t => _mlContext.MulticlassClassification
                .PermutationFeatureImportance(
                    (t.loadedData.transformer as IEnumerable<ITransformer>)!.First(),
                    t.loadedData.data,
                    t.data.Model.Features.First(f => f.IsTarget).Name,
                    permutationCount: t.data.PermutationCount)
                .OrderBy(column => column.Value.MacroAccuracy.Mean)
                .ToImmutableDictionary())
            .TapError(e =>
                _logger.LogInformation("Error getting feature importance for model {id}: {Exception}", data.Model.Id,
                    e))
            .Tap(_ => _logger.LogInformation("Successfully get feature importance for model {id}", data.Model.Id))
            .Bind(ParseMetrics);

    private Result<(IDataView data, ITransformer transformer)> TransformData(byte[]? modelData, LoadedDatasetData loadedDatasetData) =>
        Result.Try(() => _mlContext.Model
            .Load(modelData?.ToStream(), out _)
            .To(t => (t.Transform(loadedDatasetData.DataView), t)));

    private Result<List<FeatureImportanceMetric>> ParseMetrics(
        ImmutableDictionary<string, RegressionMetricsStatistics> featureStatistics) =>
        Result.Success(featureStatistics)
            .MapTry(f => f
                .Select(kvp => new FeatureImportanceMetric
                {
                    Name = kvp.Key,
                    Statistics =
                    [
                        kvp.Value.RootMeanSquaredError.ToParameterStatistics(nameof(kvp.Value.RootMeanSquaredError)),
                        kvp.Value.LossFunction.ToParameterStatistics(nameof(kvp.Value.LossFunction)),
                        kvp.Value.MeanAbsoluteError.ToParameterStatistics(nameof(kvp.Value.MeanAbsoluteError)),
                        kvp.Value.RSquared.ToParameterStatistics(nameof(kvp.Value.RSquared)),
                        kvp.Value.MeanSquaredError.ToParameterStatistics(nameof(kvp.Value.MeanSquaredError))
                    ]
                })
                .ToList());

    private Result<List<FeatureImportanceMetric>> ParseMetrics(
        ImmutableDictionary<string, BinaryClassificationMetricsStatistics> featureStatistics) =>
        Result.Success(featureStatistics)
            .MapTry(f => f
                .Select(kvp => new FeatureImportanceMetric
                {
                    Name = kvp.Key,
                    Statistics =
                    [
                        kvp.Value.Accuracy.ToParameterStatistics(nameof(kvp.Value.Accuracy)),
                        kvp.Value.F1Score.ToParameterStatistics(nameof(kvp.Value.F1Score)),
                        kvp.Value.NegativePrecision.ToParameterStatistics(nameof(kvp.Value.NegativePrecision)),
                        kvp.Value.NegativeRecall.ToParameterStatistics(nameof(kvp.Value.NegativeRecall)),
                        kvp.Value.PositivePrecision.ToParameterStatistics(nameof(kvp.Value.PositivePrecision)),
                        kvp.Value.PositiveRecall.ToParameterStatistics(nameof(kvp.Value.PositiveRecall)),
                        kvp.Value.AreaUnderRocCurve.ToParameterStatistics(nameof(kvp.Value.AreaUnderRocCurve)),
                        kvp.Value.AreaUnderPrecisionRecallCurve.ToParameterStatistics(
                            nameof(kvp.Value.AreaUnderPrecisionRecallCurve))
                    ]
                })
                .ToList());

    private Result<List<FeatureImportanceMetric>> ParseMetrics(
        ImmutableDictionary<string, MulticlassClassificationMetricsStatistics> featureStatistics) =>
        Result.Success(featureStatistics)
            .MapTry(f => f
                .Select(kvp => new FeatureImportanceMetric
                {
                    Name = kvp.Key,
                    Statistics =
                    [
                        kvp.Value.MacroAccuracy.ToParameterStatistics(nameof(kvp.Value.MacroAccuracy)),
                        kvp.Value.MicroAccuracy.ToParameterStatistics(nameof(kvp.Value.MicroAccuracy)),
                        kvp.Value.LogLoss.ToParameterStatistics(nameof(kvp.Value.LogLoss)),
                        kvp.Value.LogLossReduction.ToParameterStatistics(nameof(kvp.Value.LogLossReduction)),
                        kvp.Value.TopKAccuracy.ToParameterStatistics(nameof(kvp.Value.TopKAccuracy))
                    ]
                })
                .ToList());
}