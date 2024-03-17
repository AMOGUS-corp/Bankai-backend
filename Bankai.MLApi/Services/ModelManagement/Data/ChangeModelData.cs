using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Services.ModelManagement.Data;

public record ChangeModelData(
    Guid Id,
    string? Name = null,
    string? Description = null,
    bool? Enabled = null,
    DateTime? Modified = null,
    TimeSpan? TrainDuration = null,
    ModelEngine? ModelEngine = null,
    ModelState? State = null,
    string? Status = null,
    ModelAlgorithm? Algorithm = null,
    PredictionType? Prediction = null,
    byte[]? Data = null,
    List<HyperParameter>? HyperParameters = null,
    List<Feature>? Features = null,
    List<Metric>? Metrics = null,
    List<Dataset>? Datasets = null,
    List<FeatureImportanceMetric>? FeatureImportance = null
);
