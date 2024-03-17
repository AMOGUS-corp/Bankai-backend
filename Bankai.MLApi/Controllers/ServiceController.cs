using Asp.Versioning;
using AutoMapper;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.ModelManagement.Data;

namespace Bankai.MLApi.Controllers;

[ApiVersion("1")]
[Route("v{version:apiVersion}/service")]
[ApiController]
public class ServiceController : Controller
{
    private readonly ILogger<ServiceController> _logger;
    private readonly IModelManagementService _modelManagementService;
    private readonly IDatasetManagementService _datasetManagementService;
    private readonly IMapper _mapper;

    public ServiceController(ILogger<ServiceController> logger, IModelManagementService modelManagementService, IDatasetManagementService datasetManagementService, IMapper mapper)
    {
        _logger = logger;
        _modelManagementService = modelManagementService;
        _datasetManagementService = datasetManagementService;
        _mapper = mapper;
    }

    /// <summary>
    /// Загрузить датасет
    /// </summary>
    /// <param name="file">датасет</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    [MapToApiVersion("1")]
    [Route("datasets", Name = nameof(LoadDataset))]
    [HttpPost]
    public Task<IActionResult> LoadDataset(IFormFile file) =>
        Result.Success(file)
            .Ensure(f => f.FileName.EndsWith(".csv") || f.FileName.EndsWith(".tsv") || f.FileName.EndsWith(".txt"),
                "Only csv, tsv or txt files!")
            .Ensure(f => f.Length > 0,
                "File is empty")
            .Bind(f => _datasetManagementService.Create(new(f.FileName, "", f)))
            .Map(d => d.Id)
            .ToActionResult(StatusCodes.Status201Created);

    /// <summary>
    /// Получить список, доступных для использования, датасетов
    /// </summary>
    /// <param name="id">id датасета</param>
    /// <param name="modelId">id модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("datasets", Name = nameof(GetDatasets))]
    [HttpGet]
    public Task<IActionResult> GetDatasets(Guid? id, Guid? modelId) =>
        _datasetManagementService.Get(new(id, modelId))
            .Map(l => l.Select(d => d.Name))
            .ToActionResult();
    
    /// <summary>
    /// Изменить датасет
    /// </summary>
    /// <remarks>
    /// Пример запроса:
    ///
    ///     PUT /datasets
    ///     {
    ///         "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "name": "string",
    ///         "description": "string",
    ///         "modelIds": [
    ///             "0c8756ba-0463-4370-8a20-7df448348b3e"
    ///         ]
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("datasets", Name = nameof(UpdateDatasets))]
    [HttpPut]
    public Task<IActionResult> UpdateDatasets(ChangeDatasetData data) =>
        _datasetManagementService.Change(data)
            .Map(d => d.Id)
            .ToActionResult();

    /// <summary>
    /// Удалить датасет
    /// </summary>
    /// <param name="id">id датасета</param>
    /// <param name="modelId">id модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("datasets", Name = nameof(DeleteDatasets))]
    [HttpDelete]
    public Task<IActionResult> DeleteDatasets(Guid? id, Guid? modelId) =>
        _datasetManagementService.Delete(new(id, modelId))
            .ToActionResult(failStatusCode: StatusCodes.Status404NotFound);

    /// <summary>
    /// Активировать неактивную модель машинного обучения
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("enable-model", Name = nameof(EnableModel))]
    [HttpPost]
    public async Task<IActionResult> EnableModel(Guid id, string name = "") =>
        (await _modelManagementService.Change(new(Id: id, Enabled: true))).ToActionResult();

    /// <summary>
    /// Деактивировать неактивную модель машинного обучения, сделав ее недоступной для запросов, за исключением запроса MLServiceEnableModel.
    /// Данные о деактивированной модели могут быть просмотрены с помощью запроса MLServiceModelInfo.
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("disable-model", Name = nameof(DisableModel))]
    [HttpPost]
    public async Task<IActionResult> DisableModel(Guid id, string name = "") =>
        (await _modelManagementService.Change(new(Id: id, Enabled: false))).ToActionResult();

    /// <summary>
    /// Создание новой модели машинного обучения
    /// </summary>
    /// <param name="data">Данные для создания модели</param>
    /// <remarks>
    /// Прмиер запроса:
    ///
    ///     POST /create-model
    ///     {
    ///         "name": "Salary model",
    ///         "description": "Test model",
    ///         "engine": 1,
    ///         "predictionType": 2,
    ///         "hyperParameters": [
    ///         {
    ///             "key": "LabelColumnName",
    ///             "value": "Salary"
    ///         }
    ///         ],
    ///         "features": [
    ///         {
    ///             "name": "YearsExperience",
    ///             "type": "Single",
    ///             "isTarget": false
    ///         },
    ///         {
    ///             "name": "Salary",
    ///             "type": "Single",
    ///             "isTarget": true
    ///         }
    ///         ]
    ///     }
    /// </remarks>
    /// <returns></returns>
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [MapToApiVersion("1")]
    [Route("create-model", Name = nameof(CreateModel))]
    [HttpPost]
    public async Task<IActionResult> CreateModel(CreateModelData data) =>
        (await _modelManagementService.Create(data))
        .ToActionResult(StatusCodes.Status201Created);

    /// <summary>
    /// Загрузить модель в формате mlapi
    /// </summary>
    /// <param name="file">файл модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    [MapToApiVersion("1")]
    [Route("import-model", Name = nameof(ImportModel))]
    [HttpPost]
    public Task<IActionResult> ImportModel(IFormFile file) =>
        Result.Success(file)
            .Ensure(f => f.FileName.EndsWith(".mlapi"),
                "Only mlapi files!")
            .Ensure(f => f.Length > 0,
                "File is empty")
            .Bind(f => _modelManagementService.Import(new(f)))
            .ToActionResult(StatusCodes.Status201Created);


    /// <summary>
    /// Выгрузить модель в формате mlapi
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [DisableRequestSizeLimit]
    [MapToApiVersion("1")]
    [Route("export-model", Name = nameof(ExportModel))]
    [HttpGet]
    public Task<IActionResult> ExportModel(Guid id, string name = "") =>
        _modelManagementService.Export(new GetModelData(id, name))
            .Map(f => File(f, "application/octet-stream", $"{id}.mlapi"))
            .Finally(x => (IActionResult) x.Value);

    /// <summary>
    /// Создание копии существующей модели машинного обучения.
    /// </summary>
    /// <param name="newModelName">имя новой модели</param>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <param name="copyWeights">копировать ли веса</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [MapToApiVersion("1")]
    [Route("duplicate-model", Name = nameof(DuplicateModel))]
    [HttpPost]
    public Task<IActionResult> DuplicateModel(string newModelName, Guid id, string name = "", bool copyWeights = false) =>
        _modelManagementService.Get(new(id, name))
            .Map(l => l.First())
            .Bind(m => _modelManagementService.Create(new(newModelName,
                m.Description,
                m.Engine,
                m.Prediction,
                m.Algorithm,
                m.HyperParameters,
                m.Features,
                m.Datasets,
                copyWeights ? m.Data : null)))
            .ToActionResult(StatusCodes.Status201Created);

    /// <summary>
    /// Удалить модель машинного обучения. Для выполнения запроса необходимо корректно передать и имя, и идентификатор модели.
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <param name="removeData">удалять ли веса</param>
    /// <returns></returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [MapToApiVersion("1")]
    [Route("delete-model", Name = nameof(DeleteModel))]
    [HttpDelete]
    public Task<IActionResult> DeleteModel(Guid id, string name = "", bool removeData = false) =>
        _modelManagementService.Delete(new(id, name))
            .ToActionResult(successStatusCode: StatusCodes.Status204NoContent,
                failStatusCode: StatusCodes.Status404NotFound);

    /// <summary>
    /// Получить список моделей, доступных для вызова.
    /// </summary>
    /// <returns></returns>
    [ProducesResponseType(typeof(List<ModelDetailedInformation>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("models", Name = nameof(ModelList))]
    [HttpGet]
    public async Task<IActionResult> ModelList() =>
        (await _modelManagementService.Get(new()))
        .Map(l => l.Select(m => _mapper.Map<ModelInformation>(m)))
        .ToActionResult();


    /// <summary>
    /// Запрос на список доступных видов архитектур моделей машинного обучения.
    /// </summary>
    /// <returns></returns>
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("model-types", Name = nameof(ModelTypes))]
    [HttpGet]
    public IActionResult ModelTypes() => 
        Ok(GetValues<ModelAlgorithm>().Select(a => a.ToString()));

    /// <summary>
    /// Получить детализированную информацию по данному типу архитектуры для модели машинного обучения.
    /// </summary>
    /// <param name="modelType">арпхитектура модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("model-type-info", Name = nameof(ModelTypeInfo))]
    [HttpGet]
    public IActionResult ModelTypeInfo(string modelType)
    {
        if (string.IsNullOrWhiteSpace(modelType))
            return BadRequest("modelType is empty");

        var modelTypes = GetValues<ModelAlgorithm>().Select(a => a.ToString());
        if (!modelTypes.Contains(modelType))
            return BadRequest("This modelType is not supported");

        try
        {
            _logger.LogInformation("Trying to get hyperparameters");
            // var hyperparameters = _modelsService.GetModelTypeInfo(modelType); todo @h0tab

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return BadRequest("Incorrect mlModel type");
        }
    }

    /// <summary>
    /// Запрос на список доступных видов обучения моделей. Доступно три вида обучения: обычное обучение без валидации, сплит-обучение с
    /// автоматическим разделением на обучающую и тестовую выборку, и обучение с кросс-валидацией.
    /// </summary>
    /// <returns></returns>
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("training-types", Name = nameof(TrainingTypes))]
    [HttpGet]
    public IActionResult TrainingTypes() => 
        Ok(GetValues<TrainingMode>().Select(m => m.ToString()));

    /// <summary>
    /// Просмотр детализированной информации о данной модели.
    /// </summary>
    /// <param name="id">id модели</param>
    /// <param name="name">имя модели</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(ModelDetailedInformation), StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    [Route("model", Name = nameof(ModelInfo))]
    [HttpGet]
    public async Task<IActionResult> ModelInfo(Guid? id, string name = "") =>
        (await _modelManagementService.Get(new(id, name)))
        .Map(m => _mapper.Map<ModelDetailedInformation>(m.First()))
        .ToActionResult(failStatusCode: StatusCodes.Status404NotFound);
}
