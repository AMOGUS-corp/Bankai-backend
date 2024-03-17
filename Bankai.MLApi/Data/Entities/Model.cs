using System.ComponentModel.DataAnnotations;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Data.Entities;

public class Model
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public TimeSpan TrainDuration { get; set; }

    public ModelEngine Engine { get; set; }

    public ModelState State { get; set; } = ModelState.Empty;

    public string Status { get; set; } = String.Empty;

    public ModelAlgorithm Algorithm { get; set; }

    public PredictionType Prediction { get; set; }

    public byte[]? Data { get; set; }

    public List<HyperParameter> HyperParameters { get; set; } = new();

    public List<Feature> Features { get; set; } = new();

    public List<Metric> Metrics { get; set; } = new();

    public List<Dataset> Datasets { get; set; } = new();

    public List<FeatureImportanceMetric> FeatureImportance { get; set; } = new();
}