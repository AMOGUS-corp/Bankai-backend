using Bankai.MLApi.Services.Optimizing.Data;

namespace Bankai.MLApi.Services.Background.FeatureOptimizing;

public interface IFeatureOptimizingBackgroundService
{
    Task SendAsync(FeatureOptimizingData data);
}