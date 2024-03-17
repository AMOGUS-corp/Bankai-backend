using Asp.Versioning;
using Bankai.MLApi.Controllers.Data;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.Optimizing;
using Bankai.MLApi.Services.Training;

namespace Bankai.MLApi.Controllers;

[ApiVersion("1")]
[Route("v{version:apiVersion}/optimize")]
[ApiController]
public class OptimizeController(
    IModelManagementService modelManagementService,
    ITrainingService trainingService,
    IDatasetManagementService datasetManagementService,
    IFeatureOptimizingService featureOptimizingService,
    ILogger<OptimizeController> logger,
    MLContext mlContext)
    : Controller
{
    /// <summary>
    /// Подбор модели методом AutoML с итеративным перебором как типов модели, так и гиперпараметров
    /// </summary>
    /// <param name="request">Параметры для обучения</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     POST /all
    ///     {
    ///         "id": "0c8756ba-0463-4370-8a20-7df448348b3e",
    ///         "autoMLMode": 0,
    ///         "optimizingMetric": "MacroAccuracy",
    ///         "trainingMode": 2,
    ///         "trainingTime": 100,
    ///         "trainParameters": {
    ///             "Validation": [
    ///             {
    ///                 "name": "TestFraction",
    ///                 "value": "0.2"
    ///             }
    ///             ]
    ///         }
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status202Accepted)]
    [MapToApiVersion("1")]
    [Route("all", Name = nameof(OptimizeAll))]
    [HttpPost]
    public Task<IActionResult> OptimizeAll(OptimizeRequest request) => Optimize(request, AutoMLMode.All);

    /// <summary>
    /// Подбор гиперпараметров модели методом AutoML
    /// </summary>
    /// <param name="request">Параметры для обучения</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     POST /hyperparameters
    ///     {
    ///         "id": "0c8756ba-0463-4370-8a20-7df448348b3e",
    ///         "autoMLMode": 0,
    ///         "optimizingMetric": "MacroAccuracy",
    ///         "trainingMode": 2,
    ///         "trainingTime": 100,
    ///         "trainParameters": {
    ///             "Validation": [
    ///             {
    ///                 "name": "TestFraction",
    ///                 "value": "0.2"
    ///             }
    ///             ]
    ///         }
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status202Accepted)]
    [MapToApiVersion("1")]
    [Route("hyperparameters", Name = nameof(OptimizeHyperParameters))]
    [HttpPost]
    public Task<IActionResult> OptimizeHyperParameters(OptimizeRequest request) =>
        Optimize(request, AutoMLMode.Hyperparameters);

    /// <summary>
    /// Подбор оптимальной архитектуры модели методом AutoML
    /// </summary>
    /// <param name="request">Параметры для обучения</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     POST /model
    ///     {
    ///         "id": "0c8756ba-0463-4370-8a20-7df448348b3e",
    ///         "autoMLMode": 0,
    ///         "optimizingMetric": "MacroAccuracy",
    ///         "trainingMode": 2,
    ///         "trainingTime": 100,
    ///         "trainParameters": {
    ///             "Validation": [
    ///             {
    ///                 "name": "TestFraction",
    ///                 "value": "0.2"
    ///             }
    ///             ]
    ///         }
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status202Accepted)]
    [MapToApiVersion("1")]
    [Route("model", Name = nameof(OptimizeModel))]
    [HttpPost]
    public Task<IActionResult> OptimizeModel(OptimizeRequest request) => Optimize(request, AutoMLMode.Model);
    
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status202Accepted)]
    [MapToApiVersion("1")]
    [Route("features", Name = nameof(OptimizeFeatures))]
    [HttpPost]
    public Task<IActionResult> OptimizeFeatures(OptimizeFeaturesRequest request) =>
        modelManagementService.Get(new(request.ModelId))
            .Map(l => l.First())
            .Check(async m => await modelManagementService.Change(new(
                m.Id, 
                State: MLApi.Data.Enums.ModelState.Training,
                Status: "Optimizing features training in process",
                Modified: DateTime.Now.ToUniversalTime())))
            .Bind(async m => await featureOptimizingService.TrainModel(new (
                m, 
                request.PermutationCount, 
                request.Metric, 
                (await datasetManagementService.Load(new(ModelId: m.Id))).Value,
                async (m, service) => await service.Change(
                    new(
                        m.Id,
                        Metrics: m.Metrics.ToList(),
                        Features: m.Features,
                        HyperParameters: m.HyperParameters.ToList(),
                        Algorithm: m.Algorithm,
                        State: MLApi.Data.Enums.ModelState.Ready,
                        Status: "Ready to processing data",
                        Modified: DateTime.Now.ToUniversalTime(),
                        Data: m.Data))
                )))
            .ToActionResult(successStatusCode: StatusCodes.Status202Accepted);
    
    private Task<IActionResult> Optimize(OptimizeRequest request, AutoMLMode autoMLMode) =>
        modelManagementService.Get(new(request.Id))
            .Map(l => l.First())
            .MapTry(async m => (
                dataset: await datasetManagementService.Load(new(ModelId: m.Id)),
                model: m))
            .TapError(e => BadRequest(e))
            .Check(async t => await modelManagementService.Change(new(
                t.model.Id, 
                State: MLApi.Data.Enums.ModelState.Training,
                Status: $"AutoML training in process, expected completion time - {DateTime.Now.Add(TimeSpan.FromSeconds(request.TrainingTime)).ToUniversalTime()}",
                Modified: DateTime.Now.ToUniversalTime())))
            .Bind(t => trainingService.TrainModel(new(t.model,
                autoMLMode,
                request.OptimizingMetric,
                request.TrainingMode,
                request.TrainingTime,
                t.dataset.Value,
                request.TrainParameters,
                async (d, service) => await service.Change(
                    new(
                        t.model.Id,
                        TrainDuration: TimeSpan.FromSeconds(request.TrainingTime),
                        Metrics: d.Metrics.ToList(),
                        HyperParameters: d.HyperParameters?.ToList(),
                        Algorithm: d.ModelAlgorithm,
                        State: MLApi.Data.Enums.ModelState.Ready,
                        Status: "Ready to processing data",
                        Modified: DateTime.Now.ToUniversalTime(),
                        Data: d.Transformer?.ToByteArray(mlContext, t.dataset.Value.DataView.Schema))),
                async (e, service) => await service.Change(new(
                    t.model.Id,
                    State: MLApi.Data.Enums.ModelState.Empty,
                    Status: $"Training failed with error: {e}",
                    Modified: DateTime.Now.ToUniversalTime())))))
            .TapError(e => logger.LogError("Optimize all error: {Error}", e))
            .ToActionResult(successStatusCode: StatusCodes.Status202Accepted);
}