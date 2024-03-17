using System.ComponentModel.DataAnnotations;

namespace Bankai.MLApi.Data.Entities;

public class ParameterStatistics
{
    [Key] 
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    
    public double Mean { get; set; }

    public double StandardDeviation { get; set; }

    public double StandardError { get; set; }

    public double Count { get; set; }
}