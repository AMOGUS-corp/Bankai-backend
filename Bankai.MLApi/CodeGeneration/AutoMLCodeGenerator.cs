using System.Reflection;
using Bankai.MLApi.Common;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Models;
using Bankai.MLApi.Services.Training.Data;
using Microsoft.ML.AutoML.CodeGen;
using Microsoft.VisualBasic;

namespace Bankai.MLApi.CodeGeneration;

//TODO TESTS

public static class AutoMLCodeGenerator
{
    private const string GeneratedClassName = "Trainer";

    private static readonly Dictionary<ModelAlgorithm, string> AlgorithmTypes = new()
    {
        { SdcaRegression, "StochasticDualCoordinateAscent" },
        { LbfgsPoissonRegression, "Lbfgs" },
        { LightGbmRegression, "LightGbm" },
        { FastTreeRegression, "FastTree" },
        { FastForestRegression, "FastForest" },
        { GamRegression, "FastTree" },
        { SdcaLogisticRegressionBinary, "Sdca" },
        { SdcaNonCalibratedBinary, "Sdca" },
        { LbfgsLogisticRegressionBinary, "Lbfgs" },
        { SymbolicSgdLogisticRegressionBinary, "Sdca" },
        { LightGbmBinary, "LightGbm" },
        { FastTreeBinary, "FastTree" },
        { FastForestBinary, "FastForest" },
        { SdcaMaximumEntropyMulticlass, "Sdca" },
        { SdcaNonCalibratedMulticlass, "Sdca" },
        { LbfgsMaximumEntropyMulticlass, "LbfgsMaximumEntropy" },
        { LightGbmMulticlass, "LightGbm" },
    };

    private static readonly Dictionary<AutoMLSetting, Type> SearchSpaceOption = new()
    {
        { AutoMLSetting.LgbmSearchSpace, typeof(LgbmOption)},
        { AutoMLSetting.FastForestSearchSpace, typeof(FastForestOption)},
        { AutoMLSetting.FastTreeSearchSpace, typeof(FastTreeOption)},
        { AutoMLSetting.LbfgsLogisticRegressionSearchSpace, typeof(LbfgsOption)},
        { AutoMLSetting.LbfgsMaximumEntrophySearchSpace, typeof(LbfgsOption)},
        { AutoMLSetting.SdcaLogisticRegressionSearchSpace, typeof(SdcaOption)},
        { AutoMLSetting.SdcaMaximumEntorphySearchSpace, typeof(SdcaOption)},
    };

    private static readonly List<string> Algorithms = new() { "Sdca", "Lbfgs", "Lgbm", "FastForest", "FastTree" };

    public static string Generate(TrainingData data) =>
        $$"""
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

          namespace {{AssemblyConstants.Assembly}}
          {
              public static class {{GeneratedClassName}}
              {
                  {{data.AutoMLMode switch
                  {
                      AutoMLMode.All => GetAllClassBody(data),
                      AutoMLMode.Hyperparameters => GetHyperParametersClassBody(data),
                      AutoMLMode.Model => GetModelClassBody(data),
                      _ => ""
                  }}}
              }
          }
          """;

    #region All

    private static string GetAllClassBody(TrainingData data) =>
        $$"""
          public static async Task<TrialResult> {{TrainMethod}}(LoadedDatasetData data, MLContext mlContext, CancellationToken cancellationToken)
          {
             
            {{(data.TrainParameters is not null &&
               data.TrainParameters.Keys.Any(key => key.ToString().Contains("SearchSpace")) &&
               data.TrainingMode != TrainingMode.CrossValidation
                ? GetCustomSearchSpaceExecution(data)
                : GetExperimentExecution(data))}}
          }
          """;

    private static string GetExperimentExecution(TrainingData data) =>
        $"""
         var columnPair = data.ColumnInference.ColumnInformation.NumericColumnNames.Select(col => new InputOutputColumnPair(col)).ToArray();
         
         var pipeline =
         mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
             .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
         {
             data.Model.Prediction
                 .To(predictionType => predictionType == PredictionType.MulticlassClassification
                     ? """
                       .Append(mlContext.Transforms.Conversion.MapValueToKey(
                           inputColumnName: data.ColumnInference.ColumnInformation.LabelColumnName,
                           outputColumnName: data.ColumnInference.ColumnInformation.LabelColumnName))
                       """
                     : "")
         }
            .Append(mlContext.Auto().{data.Model.Prediction.ToString().Replace("class", "", StringComparison.InvariantCulture)}(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName));
         {
             data.TrainParameters.TryFind(AutoMLSetting.Tuner)
                 .Map(param => param.First(p => p.Name == TunerName))
                 .Match(param => param.Value, () => DefaultTunerName)
                 .To(tuner =>
                     $"""
                      return await mlContext.Auto().CreateExperiment()
                      .SetPipeline(pipeline)
                      .Set{data.Model.Prediction}Metric({data.Model.Prediction}Metric.{data.OptimizingMetric},
                          labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
                      {SetDataset(data)}
                      .SetTrainingTimeInSeconds({data.TrainingTime})
                      .Set{tuner}Tuner()
                      .RunAsync(cancellationToken);
                      """)
         }
         """;

    private static string GetCustomSearchSpaceExecution(TrainingData data) =>
        $"""
         {AppendSearchSpaces(data.TrainParameters!)}

         var columnPair = data.ColumnInference.ColumnInformation.NumericColumnNames.Select(col => new InputOutputColumnPair(col)).ToArray();
         
         var pipeline =
         mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
             .Append(mlContext.Transforms.NormalizeMinMax(columnPair))
         {
             data.Model.Prediction
                 .To(predictionType => predictionType == PredictionType.MulticlassClassification
                     ? """
                       .Append(mlContext.Transforms.Conversion.MapValueToKey(
                           inputColumnName: data.ColumnInference.ColumnInformation.LabelColumnName,
                           outputColumnName: data.ColumnInference.ColumnInformation.LabelColumnName))
                       """
                     : "")
         }
            .Append(mlContext.Auto().{data.Model.Prediction.ToString().Replace("class", "", StringComparison.InvariantCulture)}(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName
            {
                Strings.Join(data.TrainParameters!.Keys
                    .Where(key => key.ToString().Contains("SearchSpace"))
                    .Select(key => key.ToString())
                    .Select(keyString => char.ToLower(keyString.First()) + keyString[1..])
                    .Select(formattedKey => $", {formattedKey}: {formattedKey}").ToArray())
            }
            ));
         {
             data.TrainParameters.TryFind(AutoMLSetting.Tuner)
                 .Map(param => param.First(p => p.Name == TunerName))
                 .Match(param => param.Value, () => DefaultTunerName)
                 .To(tuner =>
                     $"""
                      return await mlContext.Auto().CreateExperiment()
                      .SetPipeline(pipeline)
                      .Set{data.Model.Prediction}Metric({data.Model.Prediction}Metric.{data.OptimizingMetric},
                          labelColumn: data.ColumnInference.ColumnInformation.LabelColumnName)
                      {SetDataset(data)}
                      .SetTrainingTimeInSeconds({data.TrainingTime})
                      .Set{tuner}Tuner()
                      .RunAsync(cancellationToken);
                      """)
         }
         """;

    private static string AppendSearchSpaces(Dictionary<AutoMLSetting, IEnumerable<TrainParameter>> settings) =>
        $"{Strings.Join(
            settings.Keys
                .Where(key => key.ToString().Contains("SearchSpace"))
                .Select(key => key.ToString()
                .To(keyString => char.ToLower(keyString[0]) + keyString[1..])
                .To(ss =>
                    $"""
                     var {ss} = new SearchSpace<{SearchSpaceOption[key]}>();
                     {
                         Strings.Join(SearchSpaceOption[key].GetProperties().To(props =>
                             settings[key]
                                 .Select(o => (option: o, prop: props.TryFirst(prop => prop.Name == o.Name)))
                                 .Where(t => t.prop.HasValue)
                                 .Select(t =>
                                     $"{ss}[\"{t.option.Name}\"] = new {GetUniformOption(t.prop.Value.PropertyType,t.option.Value)};"))
                             .ToArray())
                     }
                     """)).ToArray())}";

    private static string GetUniformOption(MemberInfo type, string value) =>
        type.Name switch
        {
            "Int32" => $"UniformIntOption({value})",
            "Single" => $"UniformSingleOption({value})",
            "Double" => $"UniformDoubleOption({value})",
            _ => $"ChoiceOption(\"{value}\")"
        };

    private static string SetDataset(TrainingData trainingData) =>
        $"""
                 .SetDataset({
                     trainingData.TrainingMode switch {
                         TrainingMode.Split => trainingData.TrainParameters.TryFind(AutoMLSetting.Validation)
                             .Map(param => param.First(p => p.Name == TestFraction))
                             .Match(param => param.Value, () => DefaultTestFraction)
                             .To(testFraction =>
                                 $"mlContext.Data.TrainTestSplit(data.DataView, {testFraction})"
                             ),
                         TrainingMode.CrossValidation => trainingData.TrainParameters.TryFind(AutoMLSetting.Validation)
                             .Map(param => param.First(p => p.Name == NumberOfFolds))
                             .Match(param => param.Value, () => DefaultTestFraction)
                             .To(numberOfFolds =>
                                 $"data.DataView, {numberOfFolds}"
                             ),
                         _ => "data.DataView"
                     }
                 })
         """;

    #endregion

    #region Hyperparameters

    private static string GetHyperParametersClassBody(TrainingData data) =>
        $$"""
            {{data.TrainingMode switch
            {
                TrainingMode.CrossValidation =>
                    $"public static CrossValidationExperimentResult<{data.Model.Prediction}Metrics> {TrainMethod}(LoadedDatasetData data, MLContext mlContext)",

                _ => $"public static ExperimentResult<{data.Model.Prediction}Metrics> {TrainMethod}(LoadedDatasetData data, MLContext mlContext)"
            }}}
          {
             var options = new {{data.Model.Prediction.ToString().Replace("Classification", "")}}ExperimentSettings(){
                OptimizingMetric = {{data.Model.Prediction}}Metric.{{data.OptimizingMetric}},
                MaxExperimentTimeInSeconds = {{data.TrainingTime}},
             };
             
            var experiment = mlContext.Auto().Create{{data.Model.Prediction}}Experiment(options);
             {{data.Model
                 .HyperParameters
                 .TryFirst(h => h.Key == LabelColumnName)
                 .Match(h => h.Value, () => DefaultLabelColumnName)
                 .To(label => data.TrainingMode switch
                 {
                     TrainingMode.Split =>
                         data.TrainParameters?.GetValueOrDefault(AutoMLSetting.Validation)?
                             .TryFirst(param => param.Name == TestFraction)
                             .Match(param => param.Value, () => DefaultTestFraction)
                             .To(testFraction =>
                                 $"""
                                      var splitDataView = mlContext.Data.TrainTestSplit(data.DataView, {testFraction});
                                      return experiment.Execute(trainData: splitDataView.TrainSet, validationData: splitDataView.TestSet, "{label}");
                                  """
                             ),

                     TrainingMode.CrossValidation =>
                         data.TrainParameters?.GetValueOrDefault(AutoMLSetting.Validation)?
                             .TryFirst(param => param.Name == NumberOfFolds)
                             .Match(param => param.Value, () => DefaultNumberOfFolds)
                             .To(numberOfFolds =>
                                 $"""return experiment.Execute(data.DataView, {numberOfFolds}, "{label}");"""
                             ),

                     _ => ""
                 })}}
                    }
          """;

    #endregion

    #region Model

    private static string GetModelClassBody(TrainingData data) =>
        $$"""
          public static TrialResult {{TrainMethod}}(LoadedDatasetData data, MLContext mlContext)
          {
            var options = new {{GetOptionType(data.Model.Algorithm).Name}}
            {
              {{
                  Strings.Join(
                  GetOptionType(data.Model.Algorithm)
                      .GetProperties()
                      .Select(prop =>
                          data.Model.HyperParameters.Where(hp => hp.Key == prop.Name)
                              .Select(hp => hp.Value switch
                              {
                                  _ when bool.TryParse(hp.Value, out _) =>
                                      $"{prop.Name} = {hp.Value},",

                                  _ when int.TryParse(hp.Value, out _) =>
                                      $"{prop.Name} = {hp.Value},",

                                  _ when float.TryParse(hp.Value, InvariantCulture, out _) =>
                                      $"{prop.Name} = {hp.Value}f,",

                                  _ => $"""{prop.Name} = "{hp.Value}","""
                              }))
                      .SelectMany(l => l).ToArray())
              }}
                      };
                      {{GetModelExperimentPipeline(data)}}
                      {{GetModelExperimentExecution(data)}}
                    }
          """;

    private static Type GetOptionType(ModelAlgorithm algorithm) =>
        AlgorithmTypes[algorithm] switch
        {
            "Sdca" or
                "StochasticDualCoordinateAscent" => typeof(SdcaOption),
            "Lbfgs" or
                "LbfgsMaximumEntropy" => typeof(LbfgsOption),
            "LightGbm" => typeof(LgbmOption),
            "FastTree" => typeof(FastTreeOption),
            "FastForest" => typeof(FastForestOption),
            _ => typeof(SdcaOption)
        };

    private static string GetModelExperimentPipeline(TrainingData data) =>
        data.Model.HyperParameters
            .First(x => x.Key == LabelColumnName)
            .Value
            .To(lcn => (
                lcn,
                option: GetOptionType(data.Model.Algorithm)))
            .To(t =>
                $"""
                    var pipeline = mlContext.Auto().Featurizer(data.DataView, columnInformation: data.ColumnInference.ColumnInformation)
                         .Append(mlContext.Auto().{data.Model.Prediction}(labelColumnName: data.ColumnInference.ColumnInformation.LabelColumnName, {char.ToLower(t.option.Name[0]) + t.option.Name[1..]}: options                 
                 {
                     Strings.Join(
                         Algorithms
                             .Where(a => !t.option.Name.Contains(a, StringComparison.InvariantCultureIgnoreCase))
                             .Select(a => $", use{a}: false").ToArray())
                 }));
                 """);

    private static string GetModelExperimentExecution(TrainingData data) =>
        $"{data.Model
            .HyperParameters
            .TryFirst(h => h.Key == LabelColumnName)
            .Match(h => h.Value, () => DefaultLabelColumnName)
            .To(label => data.TrainingMode switch
            {
                TrainingMode.Split =>
                    data.TrainParameters.TryFind(AutoMLSetting.Validation)
                        .Map(param => param.First(p => p.Name == TestFraction))
                        .Match(param => param.Value, () => DefaultTestFraction)
                        .To(testFraction =>
                            $"""
                             var splitDataView = mlContext.Data.TrainTestSplit(data.DataView, {testFraction});
                             return mlContext.Auto().CreateExperiment()
                                 .Set{data.Model.Prediction}Metric({data.Model.Prediction}Metric.{data.OptimizingMetric}, labelColumn: "{label}")
                                 .SetTrainingTimeInSeconds({data.TrainingTime})
                                 .SetDataset(splitDataView)
                                 .SetPipeline(pipeline)
                                 .Run();
                             """
                        ),

                TrainingMode.CrossValidation =>
                    data.TrainParameters.TryFind(AutoMLSetting.Validation)
                        .Map(param => param.First(p => p.Name == NumberOfFolds))
                        .Match(param => param.Value, () => DefaultNumberOfFolds)
                        .To(numberOfFolds =>
                            $"""
                             return mlContext.Auto().CreateExperiment()
                                .Set{data.Model.Prediction}Metric({data.Model.Prediction}Metric.{data.OptimizingMetric}, labelColumn: "{label}")
                                .SetTrainingTimeInSeconds({data.TrainingTime})
                                .SetDataset(data.DataView, {numberOfFolds})
                                .SetPipeline(pipeline)
                                .Run();
                             """
                        ),

                _ => $"""
                      return mlContext.Auto().CreateExperiment()
                          .Set{data.Model.Prediction}Metric({data.Model.Prediction}Metric.{data.OptimizingMetric}, labelColumn: "{label}")
                          .SetTrainingTimeInSeconds({data.TrainingTime})
                          .SetDataset(data.DataView)
                          .SetPipeline(pipeline)
                          .Run();
                      """
            })}";

    #endregion
}
