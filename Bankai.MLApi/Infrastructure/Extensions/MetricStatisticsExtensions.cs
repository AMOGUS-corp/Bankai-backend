using Bankai.MLApi.Data.Entities;
using Microsoft.ML.Data;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class MetricStatisticsExtensions
{
    public static ParameterStatistics ToParameterStatistics(this MetricStatistics metricStatistics, string name) =>
        new()
        {
            Name = name,
            Mean = metricStatistics.Mean,
            StandardDeviation = metricStatistics.StandardDeviation,
            StandardError = metricStatistics.StandardError,
            Count = metricStatistics.Count
        };
}