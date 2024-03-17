using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;

namespace Bankai.MLApi.Services.Training.Data;

public record TrainingData(
    Model Model,
    AutoMLMode? AutoMLMode,
    string? OptimizingMetric,
    TrainingMode TrainingMode,
    uint? TrainingTime,
    LoadedDatasetData DatasetData,
    Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>? TrainParameters,
    Func<MLMetricsData, IModelManagementService, Task<Result<Model>>>? OnSaveResult = null,
    Func<string, IModelManagementService, Task>? OnError = null);