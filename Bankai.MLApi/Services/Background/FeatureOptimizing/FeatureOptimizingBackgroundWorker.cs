using System.Threading.Tasks.Dataflow;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Services.Background.FeatureImportance;
using Bankai.MLApi.Services.Background.Training;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.Optimizing.Data;
using Microsoft.ML.AutoML;

namespace Bankai.MLApi.Services.Background.FeatureOptimizing;

public class FeatureOptimizingBackgroundWorker : IFeatureOptimizingBackgroundService
{
    private readonly ILogger<FeatureImportanceBackgroundWorker> _logger;
    private readonly ActionBlock<FeatureOptimizingData> _optimizingStream;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITrainingBackgroundService _trainingService;
    private readonly IFeatureImportanceBackgroundService _featureImportanceService;

    public FeatureOptimizingBackgroundWorker(ILogger<FeatureImportanceBackgroundWorker> logger,
        IServiceProvider serviceProvider, ITrainingBackgroundService trainingService,
        IFeatureImportanceBackgroundService featureImportanceService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _trainingService = trainingService;
        _featureImportanceService = featureImportanceService;

        _optimizingStream =
            new ActionBlock<FeatureOptimizingData>(ExecuteAsync, new() { MaxDegreeOfParallelism = 4 });
    }

    public async Task SendAsync(FeatureOptimizingData data) => await _optimizingStream.SendAsync(data);

    private async Task ExecuteAsync(FeatureOptimizingData data) =>
        await Result.Success(data)
            //TODO: Do Retrain Cycle again if accuracy falling <= 0.05
            .Check(async d => await RetrainCycle(d.Model, d.LoadedDatasetData, d.Metric, d.PermutationCount)
                .Bind(async t =>
                {
                    if (float.Parse(d.Model.Metrics.First().Value, InvariantCulture) -
                        float.Parse(t.model.Metrics.First().Value, InvariantCulture) >= 0.05)
                    {
                        t.model.Features.AddRange(t.droppedFeatures);
                        return await Retrain(t.model, d.Metric);
                    }

                    return t.model;
                })
                .Tap(async m =>
                {
                    var scope = _serviceProvider.CreateScope();
                    var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
                    if (d.OnSaveResult is not null) await d.OnSaveResult(m, modelManagementService);
                })
                .TapError(async e =>
                {
                    _logger.LogError("Feature optimizing execution of ({ModelId}) error: {Exception}", data.Model.Id, e);
                    var scope = _serviceProvider.CreateScope();
                    var modelManagementService = scope.ServiceProvider.GetRequiredService<IModelManagementService>();
                    if (data.OnError != null) await data.OnError(e, modelManagementService);
                }));

    private async Task<Result<(Model model, List<Feature> droppedFeatures)>>
        RetrainCycle(Model model, LoadedDatasetData loadedDatasetData, string metric, uint permutationCount) =>
        await _featureImportanceService
            .GetFeatureImportanceAsync(new(model, (int)permutationCount, loadedDatasetData, (_, _) => Task.CompletedTask))
            .Bind(l => GetTopFeatureImportanceMetrics(l, metric))
            .MapTry(l => (droppedFeaturesData: DropFeatures(l, model).Value, metrics: l))
            .TapError(e => _logger.LogError("Error when drop features: {Exception}", e))
            .MapTry(async t => (inference: (await PrepareInference(t.metrics, model.Id)).Value,
                t.droppedFeaturesData))
            .TapError(e => _logger.LogError("Error when preparing columns: {Exception}", e))
            .Map(async t => (model: await Retrain(t.droppedFeaturesData.model, metric, t.inference),
                t.droppedFeaturesData.droppedFeatures))
            .MapTry(t => (model: t.model.Value, t.droppedFeatures))
            .TapError(e => _logger.LogError("Error when retrain model: {Error}", e));

    private Task<Result<Model>> Retrain(Model model, string metric,
        ColumnInferenceResults? inference = null) =>
        Result.Success((inference, model))
            .Bind(async t =>
            {
                var scope = _serviceProvider.CreateScope();
                return await (await scope.ServiceProvider.GetRequiredService<IDatasetManagementService>()
                        .Load(new(ModelId: t.model.Id, ColumnInference: t.inference)))

                    .Map(async loadedData => await _trainingService.TrainModelAsync(new(
                        t.model,
                        AutoMLMode.All,
                        metric,
                        TrainingMode.Split,
                        (uint)t.model.TrainDuration.TotalSeconds,
                        loadedData,
                        new())));
            })
            .MapError(e => $"Retrain error: {e}");

    private static Result<List<FeatureImportanceMetric>> GetTopFeatureImportanceMetrics(
        List<FeatureImportanceMetric> featureImportanceMetrics,
        string metric) =>
        Result.Success(featureImportanceMetrics)
            .Tap(l => l.Sort((a, b) =>
                b.Statistics.GetStatisticAbsValue(metric).CompareTo(a.Statistics.GetStatisticAbsValue(metric))))
            .Map(l => (
                metrics: l,
                average: l.Select(f => f.Statistics.GetStatisticAbsValue(metric)).Average()))
            .Map(t => t.metrics.Where(m => m.Statistics.GetStatisticAbsValue(metric) > t.average).ToList());

    private Result<(Model model, List<Feature> droppedFeatures)> DropFeatures(
        List<FeatureImportanceMetric> featureImportance, Model model) =>
        Result.Success((featureImportance, model))
            .Map(t =>
            {
                var features = t.model.Features
                    .Where(f => t.featureImportance.Any(m => m.Name == f.Name) || f.IsTarget)
                    .Select(f => new Feature
                    {
                        IsTarget = f.IsTarget,
                        Name = f.Name,
                        Type = f.Type
                    })
                    .ToList();
                var droppedFeatures = t.model.Features
                    .Where(f => features.All(fe => fe.Name != f.Name))
                    .Select(f => new Feature
                    {
                        IsTarget = f.IsTarget,
                        Name = f.Name,
                        Type = f.Type
                    })
                    .ToList();

                return (new Model
                    {
                        Id = t.model.Id,
                        Name = t.model.Name,
                        Algorithm = t.model.Algorithm,
                        Created = t.model.Created,
                        Data = t.model.Data,
                        Features = features,
                        FeatureImportance = t.featureImportance,
                        Datasets = t.model.Datasets,
                        State = t.model.State,
                        Prediction = t.model.Prediction,
                        TrainDuration = t.model.TrainDuration,
                        Status = t.model.Status,
                        Description = t.model.Description,
                        Enabled = t.model.Enabled,
                        Engine = t.model.Engine,
                        Metrics = t.model.Metrics,
                        HyperParameters = t.model.HyperParameters,
                        Modified = t.model.Modified
                    },
                    droppedFeatures);
            })
            .Tap(t => _logger.LogInformation("Successfully dropped features: {DroppedFeatures}",
                string.Join(",", t.droppedFeatures.Select(f => f.Name))));

    private async Task<Result<ColumnInferenceResults>> PrepareInference(
        List<FeatureImportanceMetric> featureImportanceMetrics, Guid modelId) =>
        await Result.Success((featureImportanceMetrics, modelId))
            .Map(async t =>
            {
                using var scope = _serviceProvider.CreateScope();
                return (await scope.ServiceProvider.GetRequiredService<IDatasetManagementService>()
                        .Load(new(ModelId: t.modelId)))
                    .MapTry(d => (
                        columns: d.ColumnInference!.TextLoaderOptions.Columns.Where(c =>
                            t.featureImportanceMetrics.Any(f => f.Name == c.Name) ||
                            c.Name == d.ColumnInference.ColumnInformation.LabelColumnName).ToArray(),
                        loadedDatasetData: d))
                    .Map(data =>
                    {
                        data.loadedDatasetData.ColumnInference!.TextLoaderOptions.Columns.ForEach(c =>
                        {
                            if (data.columns.Any(col => col.Name == c.Name)) return;
                            data.loadedDatasetData.ColumnInference.ColumnInformation.IgnoredColumnNames.Add(c.Name);
                            data.loadedDatasetData.ColumnInference.ColumnInformation.NumericColumnNames.Remove(c.Name);
                            data.loadedDatasetData.ColumnInference.ColumnInformation.CategoricalColumnNames.Remove(
                                c.Name);
                            data.loadedDatasetData.ColumnInference.ColumnInformation.TextColumnNames.Remove(c.Name);
                        });
                        data.loadedDatasetData.ColumnInference!.TextLoaderOptions.Columns = data.columns;
                        return data.loadedDatasetData.ColumnInference!;
                    })
                    .MapError(e => $"Prepare inference error: {e}");
            });
}