using Microsoft.ML.AutoML;

namespace Bankai.MLApi.Services.DatasetManagement.Data;

public record GetDatasetData(Guid? Id = null, Guid? ModelId = null, ColumnInferenceResults? ColumnInference = null);
