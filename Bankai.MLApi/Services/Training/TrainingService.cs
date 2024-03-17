using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.Background.Training;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Services.Training;

public class TrainingService(ILogger<TrainingService> logger, ITrainingBackgroundService trainingBackgroundService)
    : ITrainingService
{
    public async Task<Result<ModelStatusInformation>> TrainModel(TrainingData data) =>
        await Result.Success(data)
            .Tap(trainingBackgroundService.SendAsync)
            .Map(d => Task.FromResult(new ModelStatusInformation
            {
                Id = d.Model.Id,
                State = ModelState.Training,
                Status = "Training in process"
            }))
            .TapError(e => logger.LogError("Error when add training: {Error}", e));

    public Result CancelTraining(Guid modelId) =>
        Result.Success(modelId)
            .MapTry(trainingBackgroundService.CancelTraining)
            .TapError(e => logger.LogError("Error when canceling training (modelId: {Id}): {Exception}", modelId, e))
            .Bind(_ => Result.Success());
}