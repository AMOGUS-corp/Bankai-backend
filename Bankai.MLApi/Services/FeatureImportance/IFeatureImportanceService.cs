using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.FeatureImportance.Data;

namespace Bankai.MLApi.Services.FeatureImportance;

public interface IFeatureImportanceService
{
     Task<Result<ModelStatusInformation>> GetFeatureImportance(GetFeatureImportanceData data);
}
