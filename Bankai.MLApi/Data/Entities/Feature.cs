using System.ComponentModel.DataAnnotations;

namespace Bankai.MLApi.Data.Entities;

public class Feature
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsTarget { get; set; }
}