using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.DatasetManagement.Data;

namespace Bankai.MLApi.Services.DatasetManagement;

public interface IDatasetManagementService
{
    Task<Result<Dataset>> Create(CreateDatasetData data);
    Task<Result<List<Dataset>>> Get(GetDatasetData data);
    Task<Result<LoadedDatasetData>> Load(GetDatasetData data);
    Task<Result<Dataset>> Change(ChangeDatasetData data);
    Task<Result<bool>> Delete(GetDatasetData data);
}