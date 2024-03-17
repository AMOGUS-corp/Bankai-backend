using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.Training.Data;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using static System.Reflection.BindingFlags;

namespace Bankai.MLApi.Services.Background.Training;

public class TrainingBackgroundWorker : ITrainingBackgroundService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<TrainingBackgroundWorker> _logger;
    private readonly ActionBlock<TrainingData> _trainingStream;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokenSources = new();

    public TrainingBackgroundWorker(MLContext mlContext, ILogger<TrainingBackgroundWorker> logger,
        IServiceProvider serviceProvider)
    {
        _mlContext = mlContext;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _trainingStream = new ActionBlock<TrainingData>(TrainModelAsync, new() { MaxDegreeOfParallelism = 4 });

        _mlContext.Log += (_, args) => { _logger.LogInformation("Context message: {RawMessage}", args.RawMessage); };
    }

    public async Task SendAsync(TrainingData data) => await _trainingStream.SendAsync(data);

    public UnitResult<string> CancelTraining(Guid modelId)
    {
        if (!_cancellationTokenSources.TryGetValue(modelId, out var cts))
            return UnitResult.Failure<string>("Error getting cancellation token");

        cts.Cancel();
        _cancellationTokenSources.TryRemove(modelId, out _);

        return UnitResult.Success<string>();
    }

    public Task<Result<Model>> TrainModelAsync(TrainingData data) =>
        (data.Model.Engine switch
        {
            ModelEngine.AutoMLNet => AutoMLTrainerFactory.Create(data),
            _ => throw new NotImplementedException($"No training type factory for {data.Model.Engine} engine")
        })
        .Tap(_ => _cancellationTokenSources.TryAdd(data.Model.Id, new CancellationTokenSource()))
        .TapError(e =>
            _logger.LogError("Training type creation for ({ModelId}) error: {Exception}", data.Model.Id, e))
        .MapTry(t => t.GetMethod(TrainMethod,
            new[] { typeof(LoadedDatasetData), typeof(MLContext), typeof(CancellationToken) })!)
        .TapError(e => _logger.LogError("Get training method error: {Exception}", e))
        .Tap(_ => _logger.LogInformation("{DateTime} - Starting training for ({ModelId})",
            DateTimeOffset.Now.ToUniversalTime(), data.Model.Id))
        .Map(method =>
        {
            _cancellationTokenSources.TryGetValue(data.Model.Id, out var cts);
            return (cts?.Token, method);
        })
        .MapTry(async t =>
        {
            var task = (Task?)t.method.Invoke(null,
                [data.DatasetData, _mlContext, t.Token]);
            await task!.ConfigureAwait(false);
            var taskResult = task.GetType().GetProperty("Result");
            return taskResult!.GetValue(task);
        })
        .TapError(async e =>
        {
            _logger.LogError("Training execution of ({ModelId}) error: {Exception}", data.Model.Id, e);
            var scope = _serviceProvider.CreateScope();
            var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
            if (data.OnError != null) await data.OnError(e, modelManagementService);
        })
        .Tap(_ => _logger.LogInformation("{DateTime} - End of training for ({ModelId})",
            DateTimeOffset.Now.ToUniversalTime(), data.Model.Id))
        .MapTry(result => data.Model.Engine switch
        {
            ModelEngine.AutoMLNet => GetAutoMLData(result!, data),
            _ => throw new NotImplementedException(
                $"Can't get metrics for {data.Model.Engine} engine")
        })!
        .Bind(async metricsData =>
        {
            var scope = _serviceProvider.CreateScope();
            var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
            if (data.OnSaveResult is not null) return await data.OnSaveResult(metricsData, modelManagementService);
            return new Model
            {
                Id = data.Model.Id,
                Name = data.Model.Name,
                Algorithm = metricsData.ModelAlgorithm ?? data.Model.Algorithm,
                Created = data.Model.Created,
                Data = metricsData.Transformer?.ToByteArray(_mlContext, data.DatasetData.DataView.Schema),
                Features = data.Model.Features,
                FeatureImportance = data.Model.FeatureImportance,
                Datasets = data.Model.Datasets,
                State = data.Model.State,
                Prediction = data.Model.Prediction,
                TrainDuration = data.Model.TrainDuration,
                Status = data.Model.Status,
                Description = data.Model.Description,
                Enabled = data.Model.Enabled,
                Engine = data.Model.Engine,
                Metrics = metricsData.Metrics.ToList(),
                HyperParameters = metricsData.HyperParameters?.ToList() ??
                                  data.Model.HyperParameters
                                      .Select(h => new HyperParameter { Key = h.Key, Value = h.Value })
                                      .ToList(),
                Modified = data.Model.Modified
            };
        })
        .Tap(_ => _cancellationTokenSources.TryRemove(data.Model.Id, out var _))
        .Tap(_ => _logger.LogInformation("Successfully saved metrics for ({ModelId})", data.Model.Id))
        .TapError(e =>
        {
            CancelTraining(data.Model.Id);
            _logger.LogError("Saving metrics of ({ModelId}) error: {Exception}", data.Model.Id, e);
        });

    #region AutoMLMetrics

    private MLMetricsData GetAutoMLData(object result, TrainingData data) =>
        result switch
        {
            _ when result.GetType().IsTypeOfGenericType(typeof(ExperimentResult<>)) =>
                GetExperimentResultData(result, data.Model.Prediction),

            _ when result.GetType().IsTypeOfGenericType(typeof(CrossValidationExperimentResult<>)) =>
                GetCrossValidationExperimentData(result, data.Model.Prediction, data.TrainParameters),

            _ => GetTrialResultData((TrialResult)result, data)
        };

    private MLMetricsData GetTrialResultData(
        TrialResult result, TrainingData data) =>
        new(new List<Metric> { new() { Name = "Metric", Value = result.Metric.ToString(InvariantCulture) } },
            GetTrialResultHyperParameters(result, data),
            null,
            GetTrialResultTransformer(result, data.DatasetData.DataView));

    private List<HyperParameter> GetTrialResultHyperParameters(TrialResult result, TrainingData data) =>
        result.TrialSettings.Parameter[Pipeline]
            .Last().Value
            .Select(kvp => new HyperParameter
            {
                Key = kvp.Key,
                Value = kvp.Value.ToString().Replace("\"", "")
            })
            .ToList();

    private ITransformer GetTrialResultTransformer(TrialResult result, IDataView dataView) =>
        (result.Model as TransformerChain<ITransformer>)!.LastTransformer.GetType().Name
        .Contains("Multiclass", StringComparison.InvariantCultureIgnoreCase)
            ? _mlContext.Transforms.Conversion.MapKeyToValue(MulticlassClassificationOutputColumn)
                .To(p => p.Fit(result.Model.Transform(dataView)))
                .To(m => result.Model.Append(m))
            : result.Model;

    private static MLMetricsData GetExperimentResultData(object result, PredictionType mlTask) =>
        mlTask switch
        {
            PredictionType.Regression => GetData((ExperimentResult<RegressionMetrics>)result),
            PredictionType.BinaryClassification => GetData((ExperimentResult<BinaryClassificationMetrics>)result),
            _ => GetData((ExperimentResult<MulticlassClassificationMetrics>)result)
        };

    private static MLMetricsData GetData<TMetrics>(
        ExperimentResult<TMetrics> result) where TMetrics : class =>
        new(GetMetrics(result.BestRun.ValidationMetrics),
            GetHyperParameters(result.BestRun.Estimator),
            Parse<ModelAlgorithm>(GetModelType(result.BestRun.TrainerName)),
            result.BestRun.Model);

    private static MLMetricsData
        GetCrossValidationExperimentData(
            object result,
            PredictionType predictionType,
            Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>? trainParams
        ) => (trainParams?.GetValueOrDefault(AutoMLSetting.Validation)?
                  .FirstOrDefault(param => param.Name == TargetMetric)
                  ?.Value
              ?? predictionType switch
              {
                  PredictionType.Regression => DefaultRegressionMetric,
                  PredictionType.BinaryClassification => DefaultBinaryClassificationMetric,
                  PredictionType.MulticlassClassification => DefaultMulticlassClassificationMetric,
                  PredictionType.Clustering => DefaultClusteringMetric,
                  _ => string.Empty
              })
        .To(targetMetric => predictionType switch
        {
            PredictionType.Regression =>
                GetData((CrossValidationExperimentResult<RegressionMetrics>)result, targetMetric),

            PredictionType.BinaryClassification =>
                GetData((CrossValidationExperimentResult<BinaryClassificationMetrics>)result, targetMetric),

            _ => GetData((CrossValidationExperimentResult<MulticlassClassificationMetrics>)result, targetMetric)
        });

    private static MLMetricsData GetData<TMetrics>(
        CrossValidationExperimentResult<TMetrics> result, string? targetMetric = null) where TMetrics : class =>
        new(GetMetrics(result.BestRun.Results
                .OrderByDescending(r => r.ValidationMetrics
                    .GetType()
                    .GetProperties()
                    .First(p => p.Name == targetMetric)
                    .GetValue(r.ValidationMetrics))
                .First().ValidationMetrics),
            GetHyperParameters(result.BestRun.Estimator),
            Parse<ModelAlgorithm>(GetModelType(result.BestRun.TrainerName)),
            result.BestRun.Results
                .OrderByDescending(r => r.ValidationMetrics
                    .GetType()
                    .GetProperties()
                    .First(p => p.Name == targetMetric)
                    .GetValue(r.ValidationMetrics))
                .First().Model);

    private static string GetModelType(string trainerName) =>
        trainerName[(trainerName.LastIndexOf("=>", StringComparison.Ordinal) + 2)..]
            .To(name => name == "Unknown"
                ? trainerName[..trainerName.LastIndexOf("=>", StringComparison.Ordinal)]
                    .To(substring =>
                        trainerName[(substring.LastIndexOf("=>", StringComparison.Ordinal) + 2)..substring.Length])
                : name);

    private static IEnumerable<Metric> GetMetrics<TMetrics>(TMetrics metrics) where TMetrics : class =>
        metrics.GetType().GetProperties()
            .Where(prop => prop.Name != "ConfusionMatrix")
            .Select(prop => new Metric { Name = prop.Name, Value = prop.GetValue(metrics)?.ToString() ?? "" })
            .ToList();

    private static IEnumerable<HyperParameter>? GetHyperParameters(IEstimator<ITransformer> estimator) =>
        (estimator
            .GetType()
            .GetFields(NonPublic | Instance)
            .First(x => x.Name.Contains("estimators"))
            .GetValue(estimator) as IEnumerable<IEstimator<ITransformer>>)?
        .FirstOrDefault(x => x.GetType().Name.Contains("Trainer"))?
        .To(trainerEstimator => trainerEstimator
            .GetType()
            .GetFields(NonPublic | Instance | Static)
            .FirstOrDefault(x => x.Name.Contains("Options", StringComparison.InvariantCultureIgnoreCase))
            ?.GetValue(trainerEstimator)
            .To(trainerOptions => trainerOptions?
                .GetType()
                .GetFields()
                .Where(field => field.GetValue(trainerOptions) is null ||
                                !field.GetValue(trainerOptions)!.GetType().IsTypeOfGenericType(typeof(IEnumerable<>)))
                .Select(field => new HyperParameter
                    { Key = field.Name, Value = field.GetValue(trainerOptions)?.ToString() ?? "" })));

    #endregion
}