using Bankai.MLApi.CodeGeneration;
using Bankai.MLApi.Data.Enums;

namespace Bankai.MLApi.Infrastructure;

public static class OutputTypeFactory
{
    public static Result<Type> Create(PredictionType predictionType, string outputType) =>
        TypeGenerator.GenerateOutputTypeCode(predictionType, outputType)
            .Bind(code => Compiler.Compile(Assembly, OutputType, code));
}