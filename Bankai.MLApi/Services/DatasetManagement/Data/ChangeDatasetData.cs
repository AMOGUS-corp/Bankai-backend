namespace Bankai.MLApi.Services.DatasetManagement.Data;

public record ChangeDatasetData(
    Guid Id,
    string? Name = null,
    string? Description = null,
    IEnumerable<Guid>? ModelIds = null
);