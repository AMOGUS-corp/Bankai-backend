using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Services.Training;

public interface ITrainingService
{
    Task<Result<ModelStatusInformation>> TrainModel(TrainingData data);

    Result CancelTraining(Guid modelId);
}
