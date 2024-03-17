using Microsoft.ML.AutoML;

namespace Bankai.MLApi.Services.DatasetManagement.Data;

public record LoadedDatasetData(IDataView DataView, ColumnInferenceResults? ColumnInference = null);
