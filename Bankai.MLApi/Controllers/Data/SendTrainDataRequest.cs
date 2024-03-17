using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Controllers.Data;

public record SendTrainDataRequest(
    Guid Id,
    TrainingMode TrainingMode,
    Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>? TrainParameters);