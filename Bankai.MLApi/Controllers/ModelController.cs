using Asp.Versioning;
using AutoMapper;
using Bankai.MLApi.Controllers.Data;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.FeatureImportance;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.Prediction;
using Bankai.MLApi.Services.Training;

namespace Bankai.MLApi.Controllers;

[ApiVersion("1")]
[Route("v{version:apiVersion}/models")]
[ApiController]
public class ModelController(
    ILogger<ModelController> logger,
    IPredictionService predictionService,
    IFeatureImportanceService featureImportanceService,
    IModelManagementService modelManagementService,
    ITrainingService trainingService,
    IDatasetManagementService datasetManagementService,
    IMapper mapper,
    MLContext mlContext)
    : Controller
{

    /// <summary>
    /// Установка новых значений гиперпараметров для модели.
    /// </summary>
    /// <param name="hyperParameters">Гиперпараметры</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <remarks>
    /// Весовые коэффициенты сбрасываются, а атрибут trained модели устанавливается в 0, что соответствует необученной модели.
    /// При установке гиперпараметров должен быть передан полный набор всех гиперпараметров, а не только те гиперпараметры, которые подлежат изменению.
    /// 
    /// Пример запроса:
    ///
    ///     POST /set-hyperparameters?id=0c8756ba-0463-4370-8a20-7df448348b3e
    ///     {
    ///         [
    ///             {
    ///                 "key": "NumberOfTrees",
    ///                 "value": "4"
    ///             },
    ///             {
    ///                 "key": "NumberOfLeaves",
    ///                 "value": "20"
    ///             }
    ///         ]
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelInformation), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("set-hyperparameters", Name = nameof(SetHyperParameters))]
    [HttpPost]
    public async Task<IActionResult>
        SetHyperParameters(List<HyperParameter> hyperParameters, Guid id, string name = "") =>
        (await modelManagementService.Change(new(Id: id, HyperParameters: hyperParameters)))
        .Map(mapper.Map<ModelInformation>)
        .ToActionResult();

    /// <summary>
    /// Добавление одного или нескольких входных параметров в модель.  
    /// </summary>
    /// <param name="features">Фичи</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <remarks>
    /// Весовые коэффициенты сбрасываются, а атрибут trained модели устанавливается в 0,
    /// что соответствует необученной модели.
    /// 
    /// Пример запроса:
    ///
    ///     POST /add-features?id=0c8756ba-0463-4370-8a20-7df448348b3e
    ///     {
    ///         [
    ///             {
    ///                 "name": "firstFeature",
    ///                 "type": "single"
    ///                 "isTarget": "false"
    ///             },
    ///             {
    ///                 "name": "targetFeature",
    ///                 "type": "single"
    ///                 "isTarget": "true"
    ///             },
    ///         ]
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelInformation), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("add-features", Name = nameof(AddFeatures))]
    [HttpPost]
    public Task<IActionResult> AddFeatures(List<Feature> features, Guid id, string name = "") =>
        modelManagementService.Get(new(id))
            .Map(l => l.First())
            .Tap(m => m.Features.AddRange(features))
            .Bind(m => modelManagementService.Change(new(Id: id, Features: m.Features)))
            .Map(mapper.Map<ModelInformation>)
            .ToActionResult();

    /// <summary>
    /// Удаление одного или нескольких входных параметров из модели.
    /// </summary>
    /// <param name="featureNames">Имена фич</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <remarks>
    /// Весовые коэффициенты сбрасываются, а атрибут trained модели устанавливается в 0,
    /// что соответствует необученной модели.
    /// 
    /// Пример запроса:
    /// 
    ///     POST /remove-features?id=0c8756ba-0463-4370-8a20-7df448348b3e
    ///     {
    ///         [
    ///            "firstFeature", "secondFeature"
    ///         ]
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelInformation), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("remove-features", Name = nameof(RemoveFeatures))]
    [HttpPost]
    public Task<IActionResult> RemoveFeatures(List<string> featureNames, Guid id, string name = "") =>
        modelManagementService.Get(new(id))
            .Map(l => l.First())
            .Tap(m => m.Features.Where(f => featureNames.Contains(f.Name))
                .ToList()
                .ForEach(f => m.Features.Remove(f)))
            .Bind(m => modelManagementService.Change(new(Id: id, Features: m.Features)))
            .Map(mapper.Map<ModelInformation>)
            .ToActionResult();

    /// <summary>
    /// Отправка пакета данных в обученную модель.
    /// </summary>
    /// <param name="inputData">Входные данные</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <returns></returns>
    /// <remarks>
    /// Для каждой фичи отдельный массив, т.е. если 2 фичи, то нужно передавать 2 массива.
    /// В каждом таком массиве должно быть равное количество передаваемых данных.
    /// 
    /// Пример запроса:
    /// 
    ///     POST /process-data?id=0c8756ba-0463-4370-8a20-7df448348b3e
    ///     {
    ///         [
    ///            ["1", "2", "3"],
    ///            ["0.1", "0.2", "0.3"]
    ///         ]
    ///     }
    /// </remarks>
    /// <exception cref="NotImplementedException"></exception>
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("process-data", Name = nameof(ProcessData))]
    [HttpPost]
    public Task<IActionResult> ProcessData(string[][] inputData, Guid? id, string name = "") =>
        Result.SuccessIf(inputData.Any(), inputData, "Input data is empty")
            .Bind(_ => modelManagementService.Get(new(id, name)))
            .Map(l => l.First())
            .Bind(m => predictionService.Predict(m, inputData))
            .ToActionResult();

    /// <summary>
    /// Обучение модели обычным ML'ом
    /// </summary>
    /// <param name="request">Параметры для обучения</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     POST /training-data
    ///     {
    ///         "id": "0c8756ba-0463-4370-8a20-7df448348b3e",
    ///         "trainingMode": 2,
    ///         "trainingTime": 10,
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
    [ProducesResponseType(typeof(IReadOnlyList<Metric>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("training-data", Name = nameof(SendTrainingData))]
    [HttpPost]
    public Task<IActionResult> SendTrainingData(SendTrainDataRequest request) =>
        modelManagementService.Get(new(request.Id))
            .Map(l => l.First())
            .MapTry(async m => (
                dataset: await datasetManagementService.Load(new(ModelId: m.Id)),
                model: m))
            .TapError(e => BadRequest(e))
            .Check(async t => await modelManagementService.Change(new(
                t.model.Id, 
                State: MLApi.Data.Enums.ModelState.Training,
                Status: $"AutoML training in process, expected completion time - {DateTime.Now.Add(TimeSpan.Zero).ToUniversalTime()}",
                Modified: DateTime.Now.ToUniversalTime())))
            .Map(t => trainingService.TrainModel(new(
                t.model,
                null,
                null,
                request.TrainingMode,
                null,
                t.dataset.Value,
                request.TrainParameters,
                async (d, service) => await service.Change(
                    new(
                        t.model.Id,
                        TrainDuration: TimeSpan.Zero,
                        Metrics: d.Metrics.ToList(),
                        HyperParameters: d.HyperParameters?.ToList(),
                        Algorithm: d.ModelAlgorithm,
                        State: MLApi.Data.Enums.ModelState.Ready,
                        Status: "Ready to processing data",
                        Modified: DateTime.Now.ToUniversalTime(),
                        Data: d.Transformer?.ToByteArray(mlContext, t.dataset.Value.DataView.Schema))),
                async (e, service) =>
                {
                    await service.Change(new(
                        t.model.Id,
                        State: MLApi.Data.Enums.ModelState.Empty,
                        Status: $"Training failed with error: {e}",
                        Modified: DateTime.Now.ToUniversalTime()));
                    logger.LogError("Training execution of ({ModelId}) error: {Exception}", t.model.Id, e);
                })))
            .ToActionResult();

    /// <summary>
    /// Получение статуса модели
    /// </summary>
    /// <param name="id">id модели</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     GET /state?id=0c8756ba-0463-4370-8a20-7df448348b3e
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("state", Name = nameof(GetModelState))]
    [HttpGet]
    public Task<IActionResult> GetModelState(Guid id) =>
        modelManagementService
            .GetStatus(id)
            .ToActionResult();

    /// <summary>
    /// Отмена обучения модели
    /// </summary>
    /// <param name="id">id модели</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     GET /cancel-training?id=0c8756ba-0463-4370-8a20-7df448348b3e
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [MapToApiVersion("1")]
    [Route("cancel-training", Name = nameof(CancelTraining))]
    [HttpGet]
    public IActionResult CancelTraining(Guid id) =>
        trainingService
            .CancelTraining(id)
            .Map(Ok).Value;

    /// <summary>
    /// Получение метрик обученной модели
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     GET /verification-results?id=0c8756ba-0463-4370-8a20-7df448348b3e
    /// </remarks>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [ProducesResponseType(typeof(IReadOnlyList<Metric>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("verification-results", Name = nameof(VerificationResults))]
    [HttpPost]
    public async Task<IActionResult> VerificationResults(Guid? id, string name = "") =>
        (await modelManagementService.Get(new(id)))
        .Map(m => m.First().Metrics)
        .ToActionResult();

    /// <summary>
    /// Определитель важностей фич в модели
    /// </summary>
    /// <param name="permutationCount">Количество перестановок</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <remarks>
    /// Пример запроса
    ///
    ///     POST /feature-importance?permutationCount=1&amp;id=0c8756ba-0463-4370-8a20-7df448348b3e
    /// </remarks>
    /// <returns>Возвращает набор параметров в зависимости от типа модели</returns>
    [ProducesResponseType(typeof(ModelStatusInformation), StatusCodes.Status202Accepted)]
    [MapToApiVersion("1")]
    [Route("feature-importance", Name = nameof(FeatureImportance))]
    [HttpPost]
    public Task<IActionResult> FeatureImportance(int permutationCount, Guid id, string name = "") =>
        modelManagementService.Get(new(id))
            .Map(l => l.First())
            .Check(async m => await modelManagementService.Change(new(
                m.Id,
                State: MLApi.Data.Enums.ModelState.CalculatingFeatureImportance,
                Status: "Calculating permutation feature importance",
                Modified: DateTime.Now.ToUniversalTime())))
            .Map(async m => (loadedData: (await datasetManagementService.Load(new(ModelId: id))).Value, model: m))
            .Map(t => featureImportanceService.GetFeatureImportance(new(
                t.model,
                permutationCount,
                t.loadedData,
                async (m, service) => await service.Change(new(
                    t.model.Id,
                    State: MLApi.Data.Enums.ModelState.Ready,
                    Status: "Ready to processing data",
                    Modified: DateTime.Now.ToUniversalTime(),
                    FeatureImportance: m)),
                async (e, service) => await service.Change(new(
                    t.model.Id,
                    State: MLApi.Data.Enums.ModelState.CalculatingFeatureImportanceFailed,
                    Status: $"Calculating permutation feature importance failed with error: {e}",
                    Modified: DateTime.Now.ToUniversalTime())))))
            .ToActionResult(successStatusCode: StatusCodes.Status202Accepted);
}