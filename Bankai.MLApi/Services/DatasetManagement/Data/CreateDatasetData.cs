namespace Bankai.MLApi.Services.DatasetManagement.Data;

public record CreateDatasetData(
    string Name,
    string Description,
    IFormFile Data
);
