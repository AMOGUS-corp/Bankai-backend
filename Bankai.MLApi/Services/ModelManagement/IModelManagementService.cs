using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Models.Dtos;
using Bankai.MLApi.Services.ModelManagement.Data;

namespace Bankai.MLApi.Services.ModelManagement;

public interface IModelManagementService
{
    Task<Result<List<Model>>> Get(GetModelData data);
    Task<Result<Guid>> Create(CreateModelData data);
    Task<Result<Model>> Change(ChangeModelData data);
    Task<UnitResult<string>> Delete(GetModelData data);
    Task<Result<Guid>> Import(ImportModelData data);
    Task<Result<byte[]>> Export(GetModelData data);
    Task<Result<ModelStatusInformation>> GetStatus(Guid id);
}