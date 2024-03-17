using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class DatasetExtensions
{
    private const string Path = "Temp/";
    private static readonly MLContext MLContext = new();

    public static Result<LoadedDatasetData> LoadData(this IEnumerable<Dataset> datasets, string name, Model model, ColumnInferenceResults? columnInference = null) =>
        Result.Try(() => CreateDirectory(Path))
            .Tap(_ => File.WriteAllBytes(Combine(Path, name), datasets.SelectMany(d => d.CompressedData).ToArray()))
            .Map(_ => model.Features)
            .Map(f => (inference: InferColumns(Combine(Path, name), f.First(f => f.IsTarget).Name), features: f))
            .Map(t => PrepareDatasetData(name, t.features, columnInference ?? t.inference));

    private static ColumnInferenceResults InferColumns(string path, string labelColumnName) =>
        MLContext.Auto()
            .InferColumns(path, labelColumnName: labelColumnName, groupColumns: false, allowQuoting: true);

    private static LoadedDatasetData PrepareDatasetData(string name, IReadOnlyCollection<Feature> features, ColumnInferenceResults i)
    {
        var target = features.First(f => f.IsTarget);
        i.ColumnInformation.NumericColumnNames.Remove("col0");
        i.TextLoaderOptions.Columns = i.TextLoaderOptions.Columns.Where(x => x.Name != "col0").ToArray();
        i.TextLoaderOptions.Columns.First(c => c.Name == target.Name).DataKind = GetValues<DataKind>()
            .First(d => string.Equals(d.ToString(), target.Type, StringComparison.CurrentCultureIgnoreCase));

        i.TextLoaderOptions.Columns.ForEach(c =>
        {
            if (features.Any(f => f.Name == c.Name)) return;
            i.ColumnInformation.IgnoredColumnNames.Add(c.Name);
            i.ColumnInformation.NumericColumnNames.Remove(c.Name);
            i.ColumnInformation.CategoricalColumnNames.Remove(c.Name);
            i.ColumnInformation.TextColumnNames.Remove(c.Name);
        });
        
        return new LoadedDatasetData(
            MLContext.Data.CreateTextLoader(i.TextLoaderOptions).Load(Combine(Path, name)), i);
    }
}