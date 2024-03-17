using System.Text.RegularExpressions;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Data.Enums;
using Bankai.MLApi.Infrastructure.Extensions;
using Microsoft.VisualBasic;

namespace Bankai.MLApi.CodeGeneration;

public static class TypeGenerator
{
    public static Result<string> GenerateInputTypeCode(Model model) =>
        $$"""
          using System;
          using Microsoft.ML.Data;
          namespace {{Assembly}}
          {
              public class {{InputType}}
              {
                  {{
                      Strings.Join(
                          model.Features
                              .To(f => {
                                  f.Sort((f1, f2) => f1.IsTarget.CompareTo(f2.IsTarget));
                                  return f;
                              })
                              .Select(feature => feature.Type switch
                              {
                                  _ when feature.Type.Contains("bool", StringComparison.InvariantCultureIgnoreCase) =>
                                      new Feature { Name = feature.Name, Type = "bool" },

                                  _ when feature.Type.Contains("single", StringComparison.InvariantCultureIgnoreCase) =>
                                      new Feature { Name = feature.Name, Type = "float" },

                                  _ => new Feature { Name = feature.Name, Type = "string" }
                              }).Select(f =>
                                  $$"""
                                    [ColumnName("{{f.Name}}")]
                                    public {{f.Type}} @{{Regex.Replace(f.Name, @"([\.,-])", "")}} { get; set; }
                                    """).ToArray())
                  }}
              }
          }
          """;
    
    //TODO: Possibly unnecessary
    public static Result<string> GenerateOutputTypeCode(PredictionType predictionType, string outputType) =>
        $$"""
          using System;
          using Microsoft.ML.Data;
          namespace {{Assembly}}
          {
             public class {{OutputType}}
             {
                 {{
                     predictionType switch
                     {
                         PredictionType.Regression =>
                             """
                             [ColumnName("Score")]
                             public float Predict { get; set; }
                             """,

                         PredictionType.BinaryClassification =>
                             """
                             [ColumnName("PredictedLabel")]
                             public float Predict { get; set; }
                             public float Score { get; set; }
                             """,

                         PredictionType.MulticlassClassification =>
                             """
                             [ColumnName("PredictedLabel")]
                             public float Predict { get; set; }
                             public float[] Score { get; set; }
                             """,

                         _ =>
                             """
                             [ColumnName("PredictedLabel")]
                             public uint Predict { get; set; }
                             public float[] Score { get; set; }
                             """
                     }
                 }}
             }
          }
          """;
}
