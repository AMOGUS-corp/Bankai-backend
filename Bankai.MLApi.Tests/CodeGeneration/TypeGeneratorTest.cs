using Bankai.MLApi.CodeGeneration;
using Bankai.MLApi.Data.Enums;
using CSharpFunctionalExtensions;

namespace Bankai.MLApi.Tests.CodeGeneration;

[TestSubject(typeof(TypeGenerator))]
public class TypeGeneratorTest : MLApiTestsBase
{
    [Fact]
    public void GenerateInputType_ReturnCorrectCode()
    {
        var model = ModelFaker.Generate();

        var sourceCode = TypeGenerator.GenerateInputTypeCode(model)
            .Map(c => WhitespaceSelector.Replace(c, ""));

        sourceCode.Value.Should().Contain(WhitespaceSelector.Replace(
            $$"""
                  using System;
                  using Microsoft.ML.Data;
                  namespace {{Assembly}}
                  {
                      public class {{InputType}}
                      {
                      [ColumnName("StringFeature")]
                      public string @StringFeature {get; set;}
                      [ColumnName("SingleFeature")]
                      public float @SingleFeature {get; set;}
                      [ColumnName("SingleLabel")]
                      public float @SingleLabel { get; set; }
                  }
                  }
              """, ""));
    }
    
    [Fact]
    public void GenerateOutputType_Regression_ReturnCorrectCode()
    {
        var sourceCode = TypeGenerator.GenerateOutputTypeCode(PredictionType.Regression, OutputType)
            .Map(c => WhitespaceSelector.Replace(c, ""));

        sourceCode.Value.Should().Contain(WhitespaceSelector.Replace(
            $$"""
                using System;
                using Microsoft.ML.Data;
                namespace {{Assembly}}
                {
                    public class {{OutputType}}
                    {
                        [ColumnName("Score")]
                        public float Predict { get; set; }
                    }
                }
              """, ""));
    }
    
    [Fact]
    public void GenerateOutputType_BinaryClassification_ReturnCorrectCode()
    {
        var sourceCode = TypeGenerator.GenerateOutputTypeCode(PredictionType.BinaryClassification, OutputType)
            .Map(c => WhitespaceSelector.Replace(c, ""));

        sourceCode.Value.Should().Contain(WhitespaceSelector.Replace(
            $$"""
                using System;
                using Microsoft.ML.Data;
                namespace {{Assembly}}
                {
                    public class {{OutputType}}
                    {
                        [ColumnName("PredictedLabel")]
                        public float Predict { get; set; }
                        public float Score { get; set; }
                    }
                }
              """, ""));
    }
    
    [Fact]
    public void GenerateOutputType_MulticlassClassification_ReturnCorrectCode()
    {
        var sourceCode = TypeGenerator.GenerateOutputTypeCode(PredictionType.MulticlassClassification, OutputType)
            .Map(c => WhitespaceSelector.Replace(c, ""));

        sourceCode.Value.Should().Contain(WhitespaceSelector.Replace(
            $$"""
                using System;
                using Microsoft.ML.Data;
                namespace {{Assembly}}
                {
                    public class {{OutputType}}
                    {
                        [ColumnName("PredictedLabel")]
                        public float Predict { get; set; }
                        public float[] Score { get; set; }
                    }
                }
              """, ""));
    }
    
    [Fact]
    public void GenerateOutputType_Clustering_ReturnCorrectCode()
    {
        var sourceCode = TypeGenerator.GenerateOutputTypeCode(PredictionType.Clustering, OutputType)
            .Map(c => WhitespaceSelector.Replace(c, ""));

        sourceCode.Value.Should().Contain(WhitespaceSelector.Replace(
            $$"""
                using System;
                using Microsoft.ML.Data;
                namespace {{Assembly}}
                {
                    public class {{OutputType}}
                    {
                        [ColumnName("PredictedLabel")]
                        public uint Predict { get; set; }
                        public float[] Score { get; set; }
                    }
                }
              """, ""));
    }
}
