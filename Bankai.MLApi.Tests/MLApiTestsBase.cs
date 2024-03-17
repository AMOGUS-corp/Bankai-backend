using System.Text.RegularExpressions;
using Bankai.MLApi.Data;
using Bankai.MLApi.Data.Entities;

namespace Bankai.MLApi.Tests;

public class MLApiTestsBase
{
    protected readonly Regex WhitespaceSelector = new (@"\s+");
    
    protected readonly MLApiDbTestContext MLApiDbTestContext = new();

    protected readonly MLApiDbContext MLApiDbContext;
        
    public MLApiTestsBase() =>
        MLApiDbContext = MLApiDbTestContext.MLApiDatabaseContext();

    protected readonly Faker<Dataset> DatasetFaker = new AutoFaker<Dataset>()
        .RuleFor(d => d.Id, Guid.NewGuid)
        .RuleFor(d => d.CompressedData, f => f.Random.Bytes(f.Random.Int(0, 100000)))
        .RuleFor(d => d.Models, new List<Model>());

    protected readonly Faker<Model> ModelFaker = new AutoFaker<Model>()
        .RuleFor(m => m.Id, Guid.NewGuid)
        .RuleFor(m => m.Name, f => f.Lorem.Word())
        .RuleFor(m => m.Description, f => f.Lorem.Sentence())
        .RuleFor(m => m.HyperParameters,
            new List<HyperParameter> { new() { Key = "LabelColumnName", Value = "Label" } })
        .RuleFor(m => m.Features, new List<Feature>
        {
            new() { Name = "StringFeature", Type = "String" },
            new() { Name = "SingleFeature", Type = "Single" },
            new() { Name = "SingleLabel", Type = "Single", IsTarget = true }
        });
}
