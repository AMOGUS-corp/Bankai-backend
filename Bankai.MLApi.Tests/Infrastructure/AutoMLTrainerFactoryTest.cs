using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Bankai.MLApi.Services.Training.Data;
using Microsoft.ML;

namespace Bankai.MLApi.Tests.Infrastructure;

[TestSubject(typeof(AutoMLTrainerFactory))]
public class AutoMLTrainerFactoryTest : MLApiTestsBase
{
    [Fact]
    public void Create_DefaultTrainingMode_TrainMethodShouldReturnExperimentResult()
    {
        var model = ModelFaker.Generate();
        model.Prediction = PredictionType.Regression;
        var trainingData = new TrainingData
        (
            model,
            AutoMLMode.All,
            "RSquared",
            TrainingMode.Default,
            5,
            null,
            new Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>()
        );

        var trainerType = AutoMLTrainerFactory.Create(trainingData);

        trainerType.Should().Succeed();
        var result = trainerType.Value;
        var methodInfo = result.GetMethod(TrainMethod, [typeof(LoadedDatasetData), typeof(MLContext), typeof(CancellationToken)
        ]);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));
    }
    
    [Fact]
    public void Create_CrossValidationTrainingMode_TrainMethodShouldReturnCrossValidationExperimentResult()
    {
        var model = ModelFaker.Generate();
        model.Prediction = PredictionType.Regression;
        var trainingData = new TrainingData
        (
            model,
            AutoMLMode.All,
            "RSquared",
            TrainingMode.CrossValidation,
            5,
            null,
            new Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>
            {
                {AutoMLSetting.Validation, new List<TrainParameter>{new("NumberOfFolds", "3")}}
            }
        );

        var trainerType = AutoMLTrainerFactory.Create(trainingData);

        trainerType.Should().Succeed();
        var result = trainerType.Value;
        var methodInfo = result.GetMethod(TrainMethod, [typeof(LoadedDatasetData), typeof(MLContext), typeof(CancellationToken)
        ]);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));
    }
    
    [Fact]
    public void Create_CustomSearchSpaces_TrainMethodShouldReturnTrialResult()
    {
        var model = ModelFaker.Generate();
        model.Prediction = PredictionType.Regression;
        var trainingData = new TrainingData
        (
            model,
            AutoMLMode.All,
            "RSquared",
            TrainingMode.Split,
            5,
            null,
            new Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>
            {
                {
                    AutoMLSetting.Validation, new List<TrainParameter>
                    {
                        new("TestFraction", "0.2")
                    }
                },
                {
                    AutoMLSetting.LgbmSearchSpace, new List<TrainParameter>
                    {
                        new("L1Regularization", "0.01f, 2.0f, false, 0.01f"),
                        new("L2Regularization", "0.01f, 2.0f, false, 0.01f")
                    }
                }
            }
        );

        var trainerType = AutoMLTrainerFactory.Create(trainingData);

        trainerType.Should().Succeed();
        var result = trainerType.Value;
        var methodInfo = result.GetMethod(TrainMethod, [typeof(LoadedDatasetData), typeof(MLContext), typeof(CancellationToken)
        ]);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));
    }
}
