using Bankai.MLApi.CodeGeneration;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Tests.CodeGeneration;

[TestSubject(typeof(AutoMLCodeGenerator))]
public class AutoMLCodeGeneratorTests : MLApiTestsBase
{
    private const string BaseCode = $$"""
                                       using System;
                                       using System.Threading;
                                       using System.Threading.Tasks;
                                       using Bankai.MLApi.Models;
                                       using Bankai.MLApi.Utils;
                                       using Bankai.MLApi.Models.Records;
                                       using Bankai.MLApi.Services.DatasetManagement.Data;
                                       using Microsoft.ML;
                                       using Microsoft.ML.Data;
                                       using Microsoft.ML.Trainers;
                                       using Microsoft.ML.Trainers.FastTree;
                                       using Microsoft.ML.Trainers.LightGbm;
                                       using Microsoft.ML.AutoML.CodeGen;
                                       using Microsoft.ML.AutoML;
                                       using Microsoft.ML.SearchSpace;
                                       using Microsoft.ML.SearchSpace.Option;
                                       using System.Linq;

                                       namespace {{Assembly}}
                                       {
                                       public static class Trainer{
                                       """;

    [Fact]
    public void Generate_AutoMLModeAll_Regression_DefaultTrainingMode_ReturnCorrectSourceCode()
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
            new()
        );

        var sourceCode = WhitespaceSelector.Replace(AutoMLCodeGenerator.Generate(trainingData), "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static async Task<TrialResult> Train(LoadedDatasetData data, MLContext mlContext, CancellationToken cancellationToken)
                {
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var columnPair = data.ColumnInference.ColumnInformation.NumericColumnNames.Select(col => new InputOutputColumnPair(col)).ToArray();
            
                var pipeline =
                mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                    .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
                    .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName));
                        
                return await mlContext.Auto().CreateExperiment()
                .SetPipeline(pipeline)
                .SetRegressionMetric(RegressionMetric.RSquared,
                    labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
                .SetDataset(data.DataView)
                .SetTrainingTimeInSeconds(5)
                .SetEciCostFrugalTuner()
                .RunAsync(cancellationToken);
                }}}
            """, ""));
    }

    [Fact]
    public void Generate_AutoMLModeAll_Regression_SplitTrainingMode_ReturnCorrectSourceCode()
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
                {AutoMLSetting.Validation, new List<TrainParameter>{new("TestFraction", "0.3")}}
            }
        );

        var sourceCode = WhitespaceSelector.Replace(AutoMLCodeGenerator.Generate(trainingData), "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static async Task<TrialResult> Train(LoadedDatasetData data, MLContext mlContext, CancellationToken cancellationToken)
                {
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
               var columnPair = data.ColumnInference.ColumnInformation.NumericColumnNames.Select(col => new InputOutputColumnPair(col)).ToArray();
            
               var pipeline =
               mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                   .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
                   .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName));
                       
               return await mlContext.Auto().CreateExperiment()
               .SetPipeline(pipeline)
               .SetRegressionMetric(RegressionMetric.RSquared,
                   labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
               .SetDataset(mlContext.Data.TrainTestSplit(data.DataView, 0.3))
               .SetTrainingTimeInSeconds(5)
               .SetEciCostFrugalTuner()
               .RunAsync(cancellationToken);
               }}}
            """, ""));
    }
    
    [Fact]
    public void Generate_AutoMLModeAll_Regression_CrossValidationTrainingMode_ReturnCorrectSourceCode()
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

        var sourceCode = WhitespaceSelector.Replace(AutoMLCodeGenerator.Generate(trainingData), "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static async Task<TrialResult> Train(LoadedDatasetData data, MLContext mlContext, CancellationToken cancellationToken)
                {
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var pipeline =
                mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                    .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
                    .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName));
                        
                return await mlContext.Auto().CreateExperiment()
                .SetPipeline(pipeline)
                .SetRegressionMetric(RegressionMetric.RSquared,
                    labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
                .SetDataset(data.DataView, 3)
                .SetTrainingTimeInSeconds(5)
                .SetEciCostFrugalTuner()
                .RunAsync(cancellationToken);
                }}}
            """, ""));
    }
    
    [Fact]
    public void Generate_AutoMLModeAll_Regression_CustomSearchSpace_ReturnCorrectSourceCode()
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
                        new("L1Regularization", "0.01f, 2.0f"),
                        new("L2Regularization", "0.01f, 2.0f")
                    }
                }
            }
        );

        var sourceCode = AutoMLCodeGenerator.Generate(trainingData);
        sourceCode = WhitespaceSelector.Replace(sourceCode, "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static async Task<TrialResult> Train(LoadedDatasetData data, MLContext mlContext, CancellationToken cancellationToken)
                {
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var lgbmSearchSpace = new SearchSpace<Microsoft.ML.AutoML.CodeGen.LgbmOption>();
                lgbmSearchSpace["L1Regularization"] = new UniformDoubleOption(0.01f, 2.0f); 
                lgbmSearchSpace["L2Regularization"] = new UniformDoubleOption(0.01f, 2.0f);
                
                var columnPair = data.ColumnInference.ColumnInformation.NumericColumnNames.Select(col => new InputOutputColumnPair(col)).ToArray();
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var pipeline =
                mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                    .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
                    .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName, lgbmSearchSpace: lgbmSearchSpace));
                        
                return await mlContext.Auto().CreateExperiment()
                .SetPipeline(pipeline)
                .SetRegressionMetric(RegressionMetric.RSquared,
                    labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
                .SetDataset(mlContext.Data.TrainTestSplit(data.DataView, 0.2))
                .SetTrainingTimeInSeconds(5)
                .SetEciCostFrugalTuner()
                .RunAsync(cancellationToken);
                }}}
            """, ""));
    }
    
    [Fact]
    public void Generate_AutoMLModeModel_Regression_DefaultTrainingMode_ReturnCorrectSourceCode()
    {
        var model = ModelFaker.Generate();
        model.Prediction = PredictionType.Regression;
        model.Algorithm = ModelAlgorithm.SdcaRegression;
        var trainingData = new TrainingData
        (
            model,
            AutoMLMode.Model,
            "RSquared",
            TrainingMode.Default,
            5,
            null,
            null
        );

        var sourceCode = WhitespaceSelector.Replace(AutoMLCodeGenerator.Generate(trainingData), "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static TrialResult Train(LoadedDatasetData data, MLContext mlContext)
                {
                   var options = new SdcaOption
                   {
                       LabelColumnName = "Label",
                   };
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var pipeline = mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                    .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName, sdcaOption: options, 
                    useLbfgs: false , useLgbm: false , useFastForest: false , useFastTree: false));
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                return mlContext.Auto().CreateExperiment()
                    .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: "Label")
                    .SetTrainingTimeInSeconds(5)
                    .SetDataset(data.DataView)
                    .SetPipeline(pipeline)
                    .Run();
            """, ""));
    }
    
    [Fact]
    public void Generate_AutoMLModeModel_Regression_SplitValidationTrainingMode_ReturnCorrectSourceCode()
    {
        var model = ModelFaker.Generate();
        model.Prediction = PredictionType.Regression;
        model.Algorithm = ModelAlgorithm.SdcaRegression;
        var trainingData = new TrainingData
        (
            model,
            AutoMLMode.Model,
            "RSquared",
            TrainingMode.Split,
            5,
            null,
            new Dictionary<AutoMLSetting, IEnumerable<TrainParameter>>
            {
                {AutoMLSetting.Validation, new List<TrainParameter>{new("TestFraction", "0.3")}}
            }
        );

        var generate = AutoMLCodeGenerator.Generate(trainingData);
        var sourceCode = WhitespaceSelector.Replace(generate, "");

        sourceCode.Should().Contain(WhitespaceSelector.Replace(BaseCode, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                public static TrialResult Train(LoadedDatasetData data, MLContext mlContext)
                {
                   var options = new SdcaOption
                   {
                       LabelColumnName = "Label",
                   };
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var pipeline = mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                    .Append(mlContext.Auto().Regression(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName, sdcaOption: options,
                    useLbfgs: false , useLgbm: false , useFastForest: false , useFastTree: false));
            """, ""));
        sourceCode.Should().Contain(WhitespaceSelector.Replace(
            """
                var splitDataView = mlContext.Data.TrainTestSplit(data.DataView, 0.3);
                return mlContext.Auto().CreateExperiment()
                    .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: "Label")
                    .SetTrainingTimeInSeconds(5)
                    .SetDataset(splitDataView)
                    .SetPipeline(pipeline)
                    .Run();
            """, ""));
    }
    
    //TODO: Add Tests For AutoMLMode HyperParameters
}
