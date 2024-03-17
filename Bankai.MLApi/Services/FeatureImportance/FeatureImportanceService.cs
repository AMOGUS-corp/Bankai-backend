using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.Background.FeatureImportance;
using Bankai.MLApi.Services.FeatureImportance.Data;

namespace Bankai.MLApi.Services.FeatureImportance;

public class FeatureImportanceService(
    ILogger<FeatureImportanceService> logger,
    IFeatureImportanceBackgroundService featureImportanceBackgroundService)
    : IFeatureImportanceService
{
    public Task<Result<ModelStatusInformation>> GetFeatureImportance(GetFeatureImportanceData data) =>
        Result.Success(data)
            .Tap(featureImportanceBackgroundService.SendAsync)
            .Map(d => Task.FromResult(new ModelStatusInformation
            {
                Id = d.Model.Id,
                State = ModelState.CalculatingFeatureImportance,
                Status = "Calculating permutation feature importance"
            }))
            .TapError(e => logger.LogError("Error when send data to feature importance background service: {Error}", e));
}