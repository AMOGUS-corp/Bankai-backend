using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Services.ModelManagement.Data;


public record ExportModelFile(
    string Name,
    ModelEngine ModelEngine, 
    ModelAlgorithm ModelAlgorithm, 
    PredictionType PredictionType,
    string Base64Data,
    List<HyperParameter> HyperParameters,
    List<Feature> Features,
    List<Metric> Metrics);