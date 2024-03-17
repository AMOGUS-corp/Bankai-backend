using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Services.Background.FeatureImportance;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.FeatureImportance;
using Bankai.MLApi.Services.FeatureImportance.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bankai.MLApi.Tests.Services.FeatureImportance;

[TestSubject(typeof(FeatureImportanceService))]
public class FeatureImportanceServiceTest : MLApiTestsBase
{
    private readonly FeatureImportanceService _sut;
    private readonly IFeatureImportanceBackgroundService _featureImportanceBackgroundService;
    private readonly Model _model;
    private readonly GetFeatureImportanceData _data;

    public FeatureImportanceServiceTest()
    {
        _model = ModelFaker.Generate();
        _data = new GetFeatureImportanceData(
            _model,
            Arg.Any<int>(),
            Arg.Any<LoadedDatasetData>());
        _featureImportanceBackgroundService = Substitute.For<IFeatureImportanceBackgroundService>();
        _sut = new(NullLogger<FeatureImportanceService>.Instance, _featureImportanceBackgroundService);
    }

    [Fact]
    public async Task GetFeatureImportance_ReturnModelStatusInformation()
    {
        _featureImportanceBackgroundService.SendAsync(_data)
            .Returns(Task.CompletedTask);

        var result = await _sut.GetFeatureImportance(_data);

        result.Should().Succeed();
        var modelStatusInformation = result.Value!;
        modelStatusInformation.Id.Should().Be(_model.Id);
        modelStatusInformation.State.Should().Be(ModelState.CalculatingFeatureImportance);
        modelStatusInformation.Status.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFeatureImportance_FeatureImportanceBackgroundServiceError_ShouldFail()
    {
        _featureImportanceBackgroundService.SendAsync(_data)
            .Returns(Task.FromCanceled(CancellationToken.None));

        var result = await _sut.GetFeatureImportance(_data);

        result.Should().Fail();
    }
}
