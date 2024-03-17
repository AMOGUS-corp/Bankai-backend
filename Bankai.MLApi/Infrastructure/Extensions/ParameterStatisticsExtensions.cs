using Bankai.MLApi.Data.Entities;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class ParameterStatisticsExtensions
{
    public static double
        GetStatisticAbsValue(this IEnumerable<ParameterStatistics> parameterStatistics, string statisticName) =>
            parameterStatistics.TryFirst(s => s.Name == statisticName)
                .Match(s => Math.Abs(s.Mean), () => 0);
}