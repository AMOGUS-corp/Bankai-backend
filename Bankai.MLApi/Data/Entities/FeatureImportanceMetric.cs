namespace Bankai.MLApi.Data.Entities;

public class FeatureImportanceMetric
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public List<ParameterStatistics> Statistics { get; set; }
}