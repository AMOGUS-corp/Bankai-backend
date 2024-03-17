using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.FeatureImportance.Data;

namespace Bankai.MLApi.Services.Background.FeatureImportance;

public interface IFeatureImportanceBackgroundService
{
    Task SendAsync(GetFeatureImportanceData data);

    Task<Result<List<FeatureImportanceMetric>>> GetFeatureImportanceAsync(GetFeatureImportanceData data);
}