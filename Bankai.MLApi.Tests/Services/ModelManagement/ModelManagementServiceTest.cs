using Bankai.MLApi.Services.ModelManagement;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bankai.MLApi.Tests.Services.ModelManagement;

[TestSubject(typeof(ModelManagementService))]
public class ModelManagementServiceTest : MLApiTestsBase
{
    private readonly ModelManagementService _sut;

    public ModelManagementServiceTest() =>
        _sut = new(MLApiDbContext, new NullLogger<ModelManagementService>());
    
    
}
