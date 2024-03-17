using System.ComponentModel.DataAnnotations;

namespace Bankai.MLApi.Data.Entities;

public class Metric
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}