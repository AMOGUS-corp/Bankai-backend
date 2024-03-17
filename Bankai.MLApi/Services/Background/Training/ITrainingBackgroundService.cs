using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Services.Background.Training;

public interface ITrainingBackgroundService
{
    Task SendAsync(TrainingData data);

    UnitResult<string> CancelTraining(Guid modelId);

    Task<Result<Model>> TrainModelAsync(TrainingData data);
}