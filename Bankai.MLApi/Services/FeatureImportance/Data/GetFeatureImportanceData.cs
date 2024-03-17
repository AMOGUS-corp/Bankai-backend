using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.ModelManagement;

namespace Bankai.MLApi.Services.FeatureImportance.Data;

public record GetFeatureImportanceData(
    Model Model,
    int PermutationCount,
    LoadedDatasetData LoadedDatasetData,
    Func<List<FeatureImportanceMetric>, IModelManagementService, Task>? OnSaveResult = null,
    Func<string, IModelManagementService, Task>? OnError = null);