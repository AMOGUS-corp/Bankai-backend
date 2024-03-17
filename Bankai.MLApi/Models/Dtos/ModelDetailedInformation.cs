using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Models.Dtos;

public class ModelDetailedInformation
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; }
    
    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public TimeSpan TrainDuration { get; set; }

    public ModelEngine Engine { get; set; }

    public ModelState State { get; set; } = ModelState.Empty;

    public ModelAlgorithm Algorithm { get; set; }

    public PredictionType Prediction { get; set; }
    
    public List<HyperParameter> HyperParameters { get; set; } = new();

    public List<Feature> Features { get; set; } = new();

    public List<Metric> Metrics { get; set; } = new();

    public List<FeatureImportanceMetric> FeatureImportance { get; set; } = new();
}