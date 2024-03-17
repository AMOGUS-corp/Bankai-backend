using Bankai.MLApi.Infrastructure;
using Microsoft.ML;

namespace Bankai.MLApi.Tests.Infrastructure;

[TestSubject(typeof(InputTypeFactory))]
public class InputTypeFactoryTest : MLApiTestsBase
{
    [Fact]
    public void Create_Correct_Types_Count_And_Sequence_Of_InputData_ReturnDataView()
    {
        var model = ModelFaker.Generate();
        var inputData = new[]
        {
            new[] { "a", "b", "c" },
            new[] { "1.2", "1.5", "2" }
        };

        var result = InputTypeFactory.Create(model, inputData, null, new MLContext());

        result.Should().Succeed();
        var dataView = result.Value;
        dataView.Schema.Count.Should().Be(3);
    }
    
    [Fact]
    public void Create_Correct_Types_And_Sequence_Of_InputData_ReturnDataView()
    {
        var model = ModelFaker.Generate();
        var inputData = new[]
        {
            new[] { "a", "b", "c" }
        };

        var result = InputTypeFactory.Create(model, inputData, null, new MLContext());

        result.Should().Succeed();
        var dataView = result.Value;
        dataView.Schema.Count.Should().Be(3);
    }
    
    [Fact]
    public void Create_Incorrect_Sequence_Of_InputData_ShouldFail()
    {
        var model = ModelFaker.Generate();
        var inputData = new[]
        {
            new[] { "1.2", "1.5", "2" },
            new[] { "a", "b", "c" }
        };

        var result = InputTypeFactory.Create(model, inputData, null, new MLContext());

        result.Should().Fail();
    }

    [Fact]
    public void Create_Incorrect_Types_Of_InputData_ShouldFail()
    {
        var model = ModelFaker.Generate();
        var inputData = new[]
        {
            new[] { "a", "b", "c" },
            new[] { "e", "f", "g" }
        };

        var result = InputTypeFactory.Create(model, inputData, null, new MLContext());

        result.Should().Fail();
    }
}
