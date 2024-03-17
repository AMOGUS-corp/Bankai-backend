using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.Background.FeatureOptimizing;
using Bankai.MLApi.Services.Optimizing.Data;

namespace Bankai.MLApi.Services.Optimizing;

public class FeatureOptimizingService(
    IFeatureOptimizingBackgroundService featureOptimizingBackgroundService,
    ILogger<FeatureOptimizingService> logger)
    : IFeatureOptimizingService
{

    public Task<Result<ModelStatusInformation>> TrainModel(FeatureOptimizingData data) =>
        Result.Success(data)
            .Tap(featureOptimizingBackgroundService.SendAsync)
            .Map(d => Task.FromResult(new ModelStatusInformation
            {
                Id = d.Model.Id,
                State = ModelState.Training,
                Status = "Optimizing features training in process"
            }))
            .TapError(e => logger.LogError("Error when send data to optimizing background service: {Error}", e));
}