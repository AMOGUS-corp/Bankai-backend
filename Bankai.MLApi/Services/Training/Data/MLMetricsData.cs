using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Services.Training.Data;

public record MLMetricsData(
    IEnumerable<Metric> Metrics,
    IEnumerable<HyperParameter>? HyperParameters,
    ModelAlgorithm? ModelAlgorithm,
    ITransformer? Transformer);