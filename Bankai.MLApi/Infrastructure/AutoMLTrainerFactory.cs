using Bankai.MLApi.CodeGeneration;
using Bankai.MLApi.Services.Training.Data;

namespace Bankai.MLApi.Infrastructure;

public static class AutoMLTrainerFactory
{
    public static Result<Type> Create(TrainingData data) =>
        Compiler.Compile(Assembly, Trainer, AutoMLCodeGenerator.Generate(data));
}
