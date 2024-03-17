using System.Collections;
using System.Runtime.Loader;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Bankai.MLApi.Infrastructure;

public static class Compiler
{
    private static readonly string CoreAssemblyLocation = typeof(object).Assembly.Location;
    private static readonly string BaseAssemblyPath = GetDirectoryName(CoreAssemblyLocation)!;
    private static readonly string AppDllPath = GetDirectoryName(typeof(Model).Assembly.Location)!;
    private static readonly MetadataReference[] References =
    {
        CreateFromFile(CoreAssemblyLocation),
        CreateFromFile(typeof(DictionaryBase).Assembly.Location),
        CreateFromFile(typeof(ColumnNameAttribute).Assembly.Location),
        CreateFromFile(typeof(KMeansTrainer).Assembly.Location),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.DataView.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.StandardTrainers.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.FastTree.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.LightGbm.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.Transforms.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.Core.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.AutoML.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.Data.dll")),
        CreateFromFile(Combine(AppDllPath, "Microsoft.ML.SearchSpace.dll")),
        CreateFromFile(Combine(AppDllPath, "Bankai.MLApi.dll")),
        CreateFromFile(Combine(BaseAssemblyPath, "System.Console.dll")),
        CreateFromFile(Combine(BaseAssemblyPath, "netstandard.dll")),
        CreateFromFile(Combine(BaseAssemblyPath, "System.Runtime.dll")),
        CreateFromFile(Combine(BaseAssemblyPath, "System.ComponentModel.dll")),
        CreateFromFile(Combine(BaseAssemblyPath, "System.Linq.dll")),
    };

    public static Result<Type> Compile(string assemblyName, string className, string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode)) return Result.Failure<Type>("Source code must not be empty");
        
        using var stream = new MemoryStream();
        
        var emitResult = CSharpCompilation.Create(
                assemblyName: assemblyName + Guid.NewGuid() + ".dll",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(sourceCode) },
                references: References,
                options: new(OutputKind.DynamicallyLinkedLibrary))
            .Emit(stream);

        return emitResult.Success
            ? Result.Success(stream)
                .Map(s => {
                    s.Seek(0, SeekOrigin.Begin);
                    return s;
                })
                .Map(s => AssemblyLoadContext.Default
                    .LoadFromStream(s)
                    .GetType($"{assemblyName}.{className}")!)
            : Result.Failure<Type>(emitResult.Diagnostics
                .Select(d => $"{d.Id} {d.GetMessage()}")
                .To(s => string.Join(NewLine, s)));
    }
}
