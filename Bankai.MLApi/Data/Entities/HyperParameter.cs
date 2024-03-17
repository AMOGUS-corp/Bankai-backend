using System.ComponentModel.DataAnnotations;

namespace Bankai.MLApi.Data.Entities;

public class HyperParameter
{
    [Key]
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}