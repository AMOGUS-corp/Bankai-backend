using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Controllers.Data;

public record OptimizeRequest(
    Guid Id,
    string OptimizingMetric,
    TrainingMode TrainingMode,
    uint TrainingTime,
    Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>? TrainParameters);
