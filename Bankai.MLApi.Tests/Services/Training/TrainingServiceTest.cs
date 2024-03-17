using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Services.Background.Training;
using Bankai.MLApi.Services.Training;
using Bankai.MLApi.Services.Training.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bankai.MLApi.Tests.Services.Training;

[TestSubject(typeof(TrainingService))]
public class TrainingServiceTest : MLApiTestsBase
{
    private readonly TrainingService _sut;
    private readonly ITrainingBackgroundService _trainingBackgroundService;
    private readonly TrainingData _trainingData;
    private readonly Model _model;

    public TrainingServiceTest()
    {
        _model = ModelFaker.Generate();
        _trainingData = new TrainingData(
            _model,
            AutoMLMode.All,
            "RSquared",
            TrainingMode.Default,
            5,
            null,
            new());
        _trainingBackgroundService = Substitute.For<ITrainingBackgroundService>();
        _sut = new(NullLogger<TrainingService>.Instance, _trainingBackgroundService);
    }

    [Fact]
    public async Task TrainModel_ReturnModelStatusInformation()
    {
        _trainingBackgroundService.SendAsync(_trainingData)
            .Returns(Task.CompletedTask);

        var result = await _sut.TrainModel(_trainingData);

        result.Should().Succeed();
        var modelStatusInformation = result.Value!;
        modelStatusInformation.Id.Should().Be(_model.Id);
        modelStatusInformation.State.Should().Be(ModelState.Training);
        modelStatusInformation.Status.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TrainModel_TrainingBackgroundServiceError_ShouldFail()
    {
        _trainingBackgroundService.SendAsync(_trainingData)
            .Returns(Task.FromCanceled(CancellationToken.None));
        
        var result = await _sut.TrainModel(_trainingData);

        result.Should().Fail();
    }
}