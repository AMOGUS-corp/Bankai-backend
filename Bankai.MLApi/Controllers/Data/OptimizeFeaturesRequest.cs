namespace Bankai.MLApi.Controllers.Data;

public record OptimizeFeaturesRequest(
    Guid ModelId,
    string Metric,
    uint PermutationCount,
    string ModelName = ""
);