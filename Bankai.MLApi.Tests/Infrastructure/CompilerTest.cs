using Bankai.MLApi.Infrastructure;

namespace Bankai.MLApi.Tests.Infrastructure;

[TestSubject(typeof(Compiler))]
public class CompilerTest : MLApiTestsBase
{
    [Fact]
    public void Compile_CorrectSourceCode_ReturnType()
    {
        var sourceCode =
            $$"""
            using System;
            
            namespace {{Assembly}}{
            
            public class Test{}
            }
            """;

        var type = Compiler.Compile(Assembly, "Test", sourceCode);

        type.Should().Succeed();
        var result = type.Value;
        result.Assembly.FullName.Should().Contain(Assembly);
        result.Name.Should().Be("Test");
    }
    
    [Fact]
    public void Compile_IncorrectSourceCode_ShouldFail()
    {
        var sourceCode = "Incorrect source code";

        var type = Compiler.Compile(Assembly, "Test", sourceCode);

        type.Should().Fail();
    }
    
    [Fact]
    public void Compile_NoInput_ShouldSucceed()
    {
        var type = Compiler.Compile("", "", "");

        type.Should().Fail();
    }
}
