using Bankai.MLApi.CodeGeneration;
using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure.Extensions;
using Microsoft.ML.Data;
using static System.Activator;

namespace Bankai.MLApi.Infrastructure;

public static class InputTypeFactory
{
    public static Result<IDataView> Create(Model model, string[][] inputData, string[][]? outputData, MLContext mlContext) =>
        CreateData(model, inputData, outputData)
            .Map(input => (IDataView) mlContext.Data
                .GetType()
                .GetMethods()
                .First(x => x is { Name: "LoadFromEnumerable", IsGenericMethod: true })
                .MakeGenericMethod(input.InputType)
                .Invoke(mlContext.Data, new object?[] { input.InputList, SchemaDefinition.Create(input.InputType) })!);
    
    private static Result<InputData> CreateData(Model model, string[][] inputData, string[][]? outputData) =>
        TypeGenerator.GenerateInputTypeCode(model)
            .Bind(code => Compiler.Compile(Assembly,InputType, code))
            .Map(it => (
                itemType: it,
                itemList: (IEnumerable<object>)
                CreateInstance(typeof(List<>)
                    .MakeGenericType(it))!))
            .Map(t => (
                t.itemType,
                t.itemList,
                addAction: GetAddAction(t.itemList)))
            .TapTry(t => Range(0, inputData.First().Length)
                .ForEach(i => {
                    List<object> objects = new();

                    inputData.ForEach(x => AddValueToObjectsList(x[i], objects));

                    if (outputData is not null && model.Algorithm != KMeans)
                        outputData.ForEach(x => AddValueToObjectsList(x[i], objects));

                    t.addAction.Invoke(objects.ToArray());
                }))
            .MapError(e => $"Error while adding values to list of input types: {e}")
            .Map(t => new InputData(t.itemType, t.itemList));

    private static void AddValueToObjectsList(string data, ICollection<object> objects)
    {
        if (bool.TryParse(data, out var boolValue))
            objects.Add(boolValue);
        else if (float.TryParse(data, InvariantCulture, out var floatValue))
            objects.Add(floatValue);
        else
            objects.Add(data);
    }

    private static Action<T[]> GetAddAction<T>(IEnumerable<T> list) =>
        values => list.GetType().GenericTypeArguments.First()
            .To(itemType => CreateInstance(itemType)
                .To(item => {
                    Range(0, values.Length).ForEach(i => itemType.GetProperties()[i].SetValue(item, values[i]));
                    return list.GetType().GetMethod(AddMethod)!.Invoke(list, new[] { item });
                }));
}
