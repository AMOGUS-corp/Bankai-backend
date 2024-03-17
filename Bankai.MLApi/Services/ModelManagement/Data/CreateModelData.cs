using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Services.ModelManagement.Data;

public record CreateModelData(
    string Name,
    string Description,
    ModelEngine Engine,
    PredictionType PredictionType,
    ModelAlgorithm? ModelAlgorithm,
    IEnumerable<HyperParameter> HyperParameters,
    IEnumerable<Feature>? Features,
    IEnumerable<Dataset>? Datasets,
    byte[]? Data);
