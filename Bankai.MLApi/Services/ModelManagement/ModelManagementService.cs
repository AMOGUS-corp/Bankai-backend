using Bankai.MLApi.Data;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.ModelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Bankai.MLApi.Services.ModelManagement;

public class ModelManagementService(MLApiDbContext dbContext, ILogger<ModelManagementService> logger)
    : IModelManagementService
{
    public Task<Result<List<Model>>> Get(GetModelData data) =>
        Result.Success(data)
            .Tap(d => logger.LogInformation("Getting models with id: {Id}, name: {name}", d.Id, d.Name))
            .MapTry(async d => await dbContext.Models
                .Where(m => m.Enabled
                            && (d.Id == null && d.Name.IsNullOrWhiteSpaces()
                                || d.Id != null && d.Id == m.Id
                                || !d.Name.IsNullOrWhiteSpaces() && d.Name == m.Name))
                .Include(m => m.HyperParameters)
                .Include(m => m.Features)
                .Include(m => m.Metrics)
                .Include(m => m.FeatureImportance)
                .ThenInclude(fi => fi.Statistics)
                .ToListAsync())
            .Ensure(l => l.Count > 0, "Model not found")
            .Tap(l => logger.LogInformation("Successfully get models ({ModelId})",
                string.Join(",", l.Select(m => m.Id))))
            .TapError(e => logger.LogError("Error while get models: {Exception}", e));

    public Task<Result<Guid>> Create(CreateModelData data) =>
        Result.Success(data)
            .MapTry(async d => (await dbContext.Models
                .AddAsync(new()
                {
                    Name = d.Name,
                    Description = d.Description,
                    Enabled = true,
                    Created = DateTime.Now.ToUniversalTime(),
                    Engine = d.Engine,
                    State = ModelState.Empty,
                    Status = "Empty model",
                    Algorithm = d.ModelAlgorithm ?? SdcaRegression,
                    Prediction = d.PredictionType,
                    HyperParameters = d.HyperParameters.ToList(),
                    Features = d.Features?.ToList() ?? Empty<Feature>().ToList()
                })).Entity.Id)
            .TapTry(async _ => await dbContext.SaveChangesAsync())
            .Tap(id => logger.LogInformation("Successfully created model ({ModelId})", id))
            .MapError(e =>
            {
                logger.LogError("Create model error: {Exception}", e);
                return $"Create model error: {e}";
            });

    public Task<Result<Model>> Change(ChangeModelData data) =>
        Result.Success(data)
            .MapTry(async d => (
                data: d,
                model: (await Get(new(d.Id))).Value.First()))
            .Map(t =>
            {
                t.model.Name = t.data.Name ?? t.model.Name;
                t.model.Description = t.data.Description ?? t.model.Description;
                t.model.Enabled = t.data.Enabled ?? t.model.Enabled;
                t.model.Created = t.model.Created;
                t.model.Modified = t.data.Modified ?? t.model.Modified;
                t.model.TrainDuration = t.data.TrainDuration ?? t.model.TrainDuration;
                t.model.Engine = t.data.ModelEngine ?? t.model.Engine;
                t.model.State = t.data.State ?? t.model.State;
                t.model.Status = t.data.Status ?? t.model.Status;
                t.model.Algorithm = t.data.Algorithm ?? t.model.Algorithm;
                t.model.Prediction = t.data.Prediction ?? t.model.Prediction;
                t.model.Data = t.data.Data ?? t.model.Data;
                t.model.HyperParameters = t.data.HyperParameters ?? t.model.HyperParameters;
                t.model.Features = t.data.Features ?? t.model.Features;
                t.model.Metrics = t.data.Metrics ?? t.model.Metrics;
                t.model.Datasets = t.data.Datasets ?? t.model.Datasets;
                t.model.FeatureImportance = t.data.FeatureImportance ?? t.model.FeatureImportance;
                return t.model;
            })
            .TapTry(m => dbContext.Models.Update(m))
            .TapTry(async _ => await dbContext.SaveChangesAsync())
            .Tap(m => logger.LogInformation("Model ({ModelId}) was successfully updated", m.Id))
            .TapError(e => logger.LogInformation("Error updating model: {Exception}", e));

    public Task<UnitResult<string>> Delete(GetModelData data) =>
        Result.Success(data)
            .Map(Get)
            .MapError(e => $"Deleting model error: {e}")
            .MapTry(m => m.First())
            .TapTry(m => dbContext.Models.Remove(m))
            .TapTry(async _ => await dbContext.SaveChangesAsync())
            .Tap(m => logger.LogInformation("Model ({ModelId}) was successfully deleted", m.Id))
            .TapError(e => logger.LogError("Error delete model: {Exception}", e))
            .Match(_ => UnitResult.Success<string>(), e => UnitResult.Failure(e));

    public Task<Result<Guid>> Import(ImportModelData data) =>
        Result.Success(data)
            .MapTry(d =>
            {
                using var memoryStream = new MemoryStream();
                d.Data.CopyTo(memoryStream);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            })
            .MapTry(JsonConvert.DeserializeObject<ExportModelFile>)
            .MapTry(async d => (await dbContext.Models
                .AddAsync(new()
                {
                    Name = d!.Name,
                    Enabled = true,
                    Created = DateTime.Now.ToUniversalTime(),
                    Engine = d.ModelEngine,
                    State = ModelState.Ready,
                    Status = "Ready to processing data",
                    Algorithm = d.ModelAlgorithm,
                    Prediction = d.PredictionType,
                    HyperParameters = d.HyperParameters.Select(x => new HyperParameter
                    {
                        Key = x.Key,
                        Value = x.Value
                    }).ToList(),
                    Features = d.Features.Select(x => new Feature
                    {
                        IsTarget = x.IsTarget,
                        Name = x.Name,
                        Type = x.Type
                    }).ToList(),
                    Metrics = d.Metrics.Select(x => new Metric
                    {
                        Name = x.Name,
                        Value = x.Value
                    }).ToList(),
                    Data = Convert.FromBase64String(d.Base64Data)
                })).Entity.Id)
            .TapTry(async _ => await dbContext.SaveChangesAsync())
            .Tap(id => logger.LogInformation("Model ({ModelId}) was successfully imported", id))
            .TapError(e => logger.LogError("Error import model: {Exception}", e));

    public Task<Result<byte[]>> Export(GetModelData data) =>
        Result.Success(data)
            .Map(Get)
            .MapTry(m => m.First())
            .MapTry(m => JsonConvert.SerializeObject(new ExportModelFile(
                m.Name,
                m.Engine,
                m.Algorithm,
                m.Prediction,
                Convert.ToBase64String(m.Data!),
                m.HyperParameters,
                m.Features,
                m.Metrics)))
            .MapTry(Encoding.UTF8.GetBytes)
            .Tap(_ => logger.LogInformation("Model ({ModelId}) was successfully exported", data.Id))
            .TapError(e => logger.LogError("Error export model: {Exception}", e));

    public Task<Result<ModelStatusInformation>> GetStatus(Guid id) =>
        Result.Success(id)
            .MapTry(modelId => dbContext.Models.FirstAsync(m => m.Id == modelId))
            .Tap(m => logger.LogInformation($"Model {m.Id} has current status: {m.State} - {m.Status}"))
            .TapError(m => logger.LogError($"Can`t get model status({id}), {m}"))
            .Map(m => new ModelStatusInformation
            {
                Id = m.Id,
                State = m.State,
                Status = m.Status
            });
}