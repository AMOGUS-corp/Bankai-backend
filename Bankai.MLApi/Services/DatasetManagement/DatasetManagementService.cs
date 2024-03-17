using Bankai.MLApi.Data;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace Bankai.MLApi.Services.DatasetManagement;

public class DatasetManagementService(MLApiDbContext dbContext, ILogger<DatasetManagementService> logger)
    : IDatasetManagementService
{
    public Task<Result<Dataset>> Create(CreateDatasetData data) =>
        Result.SuccessIf(IsValidDataset(data), data, "Invalid dataset")
            .MapTry(async d => (await dbContext.Datasets
                .AddAsync(new()
            {
                Name = d.Name,
                Description = d.Description,
                CompressedData = ConvertFileToBytes(d.Data)
            })).Entity)
            .TapTry(async _ => await dbContext.SaveChangesAsync())
            .Tap(_ => logger.LogInformation("Successfully created dataset"))
            .TapError(e => logger.LogError("Error creating dataset: {Exception}", e));


    public Task<Result<List<Dataset>>> Get(GetDatasetData data) =>
        Result.Success(data)
            .Ensure(d => d.Id is not null || d.ModelId is not null,
                "Id or ModelId must not be null")
            .MapTry(dat => dbContext.Datasets
                .Where(d => dat.Id != null && d.Id == dat.Id!.Value
                    || dat.ModelId != null && d.Models.Select(m => m.Id).Contains(dat.ModelId!.Value))
                .Include(d => d.Models)
                .ThenInclude(m => m.Features)
                .ToListAsync())
            .Ensure(l => l.Count > 0, "Dataset not found")
            .TapError(e => logger.LogError("Error getting datasets: {Exception}", e));

    public Task<Result<LoadedDatasetData>> Load(GetDatasetData data) =>
        Get(new(ModelId: data.ModelId))
            .MapError(e => $"Load dataset error: {e}")
            .MapTry(l => l.Where(d => d.Models.Any(m => m.Id == data.ModelId))
                .ToList())
            .Map(l => (model: l.First().Models.First(m => m.Id == data.ModelId), datasets: l))
            .Bind(t => t.datasets.LoadData(t.datasets.First().Name, t.model, data.ColumnInference))
            .TapError(e => logger.LogError("Error load datasets: {Exception}", e));
    
    public Task<Result<Dataset>> Change(ChangeDatasetData data) =>
        Get(new(data.Id))
            .Map(l => l.First())
            .Map(d =>
            {
                d.Name = data.Name ?? d.Name;
                d.Description = data.Description ?? d.Description;
                d.Models = data.ModelIds is null ? d.Models 
                    : dbContext.Models.Where(m => data.ModelIds!.Contains(m.Id)).ToList();
                return d;
            })
            .TapTry(d => dbContext.Datasets.Update(d))
            .TapTry(_ => dbContext.SaveChangesAsync())
            .TapTry(_ => dbContext.ChangeTracker.Clear())
            .Tap(d => logger.LogInformation("Successfully updated dataset ({DatasetId})", d.Id))
            .TapError(e => logger.LogError("Error updating dataset: {Exception}", e));

    public Task<Result<bool>> Delete(GetDatasetData data) =>
        Result.Success(data)
            .Map(Get)
            .MapError(e => $"Deleting dataset error: {e}")
            .MapTry(l => l.First())
            .TapTry(d => dbContext.Remove(d))
            .TapTry(_ => dbContext.SaveChangesAsync())
            .Tap(d => logger.LogInformation("Successfully deleted dataset ({DatasetId})", d.Id))
            .TapError(e => logger.LogError("Error deleting dataset: {Exception}", e))
            .Bind(_ => Result.Success(true));
    
    private static bool IsValidDataset(CreateDatasetData data) =>
        Result.Success(data)
            .MapTry(d =>
            {
                var delimiters = new[] { ",", ";", "\t" };
                var buffer = ConvertFileToBytes(d.Data);
                foreach (var delimiter in delimiters)
                {
                    using var memoryStream = new MemoryStream(buffer);
                    using var parser = new TextFieldParser(memoryStream);
    
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.SetDelimiters(delimiter);
    
                    var headers = parser.ReadFields();
                    if (headers is null || headers.Length <= 1) continue;
    
                    while (!parser.EndOfData)
                    {
                        if (parser.ReadFields()?.Length != headers?.Length)
                            return false;
                    }
    
                    return true;
                }
    
                return false;
            }).Value;

    private static byte[] ConvertFileToBytes(IFormFile file)
    {
        using var ms = new MemoryStream((int) file.Length);
        
        file.CopyTo(ms);
        // todo 7zip
        return ms.ToArray();
    }
}
