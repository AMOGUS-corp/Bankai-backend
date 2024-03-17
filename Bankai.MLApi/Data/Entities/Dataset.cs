using System.ComponentModel.DataAnnotations;

namespace Bankai.MLApi.Data.Entities;

public class Dataset
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = String.Empty;

    [MaxLength(512)]
    public string Description { get; set; } = String.Empty;

    public byte[] CompressedData { get; set; } = null!;

    public List<Model> Models { get; set; } = new();
}