using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.Optimizing.Data;

namespace Bankai.MLApi.Services.Optimizing;

public interface IFeatureOptimizingService
{
    Task<Result<ModelStatusInformation>> TrainModel(FeatureOptimizingData data);
}