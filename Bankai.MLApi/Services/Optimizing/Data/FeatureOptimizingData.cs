using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;

namespace Bankai.MLApi.Services.Optimizing.Data;

public record FeatureOptimizingData(
    Model Model,
    uint PermutationCount,
    string Metric,
    LoadedDatasetData LoadedDatasetData,
    Func<Model, IModelManagementService, Task>? OnSaveResult = null,
    Func<string, IModelManagementService, Task>? OnError = null);