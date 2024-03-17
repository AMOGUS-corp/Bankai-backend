using Bankai.MLApi.Data.Entities;
using Bankai.MLApi.Infrastructure.Extensions;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.DatasetManagement.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bankai.MLApi.Tests.Services.DatasetManagement;

[TestSubject(typeof(DatasetManagementService))]
public class DatasetManagementServiceTest : MLApiTestsBase
{
    private readonly DatasetManagementService _sut;

    public DatasetManagementServiceTest() =>
        _sut = new(MLApiDbContext, new NullLogger<DatasetManagementService>());

    // TODO: Fix
     [Fact]
     public async Task CreateTest()
     {
         var dataset = DatasetFaker.Generate();
         using var ms = dataset.CompressedData.ToStream();
         var request = new CreateDatasetData(
             dataset.Name,
             dataset.Description,
             new FormFile(ms,
                 0,
                 dataset.CompressedData.Length,
                 dataset.Name,
                 dataset.Name));
    
         var result = await _sut.Create(request);
    
         result.Should().Succeed();
         var resultValue = result.Value;
         resultValue.Id.Should().NotBeEmpty();
         resultValue.Name.Should().Be(dataset.Name);
         resultValue.Description.Should().Be(dataset.Description);
         resultValue.CompressedData.Should().BeEquivalentTo(dataset.CompressedData);
         resultValue.Models.Should().BeEmpty();
         var resultEntity = await MLApiDbContext.Datasets.Include(d => d.Models).FirstAsync();
         resultEntity.Id.Should().NotBeEmpty();
         resultEntity.Name.Should().Be(resultValue.Name);
         resultEntity.Description.Should().Be(resultValue.Description);
         resultEntity.CompressedData.Should().BeEquivalentTo(resultValue.CompressedData);
         resultEntity.Models.Should().BeEmpty();
     }

    [Fact]
    public async Task CreateFail_Test()
    {
        MLApiDbTestContext.Dispose();
        var result = await _sut.Create(new("", "", new FormFile(Substitute.For<Stream>(), 0, 0, "", "")));

        result.Should().Fail();
    }

    [Fact]
    public async Task GetByIdTest()
    {
        var datasets = DatasetFaker.Generate(5);
        await MLApiDbTestContext.ChangeAsync(d => d.Datasets.AddRangeAsync(datasets));

        var result = await _sut.Get(new(datasets[0].Id));

        result.Should().Succeed();
        var resultValue = result.Value;
        resultValue.Should().HaveCount(1);
        AssertDatasets(datasets[0], resultValue.First());
    }

    [Fact]
    public async Task GetByModelIdTest()
    {
        var modelId = Guid.NewGuid();
        var datasetsWithoutModel = DatasetFaker.Generate(3);
        var datasetsWithModel = DatasetFaker
            .RuleFor(d => d.Models, new List<Model> { new() { Id = modelId } })
            .Generate(2);
        var datasets = datasetsWithModel.Concat(datasetsWithoutModel).ToList();
        await MLApiDbTestContext.ChangeAsync(d => d.Datasets.AddRangeAsync(datasets));

        var result = await _sut.Get(new(ModelId: modelId));

        result.Should().Succeed();
        var resultValue = result.Value;
        resultValue.Should().HaveCount(2);
        resultValue.ForEach((d, i) => AssertDatasets(datasets[i], d));
    }

    [Fact]
    public async Task GetFail_NoInput_Test() =>
        (await _sut.Get(new())).Should().FailWith("Id or ModelId must not be null");
    
    [Fact]
    public async Task GetFail_DbException_Test()
    {
        MLApiDbTestContext.Dispose();
        (await _sut.Get(new())).Should().Fail();
    }

    private static void AssertDatasets(Dataset expected, Dataset actual)
    {
        expected.Id.Should().Be(actual.Id);
        expected.Name.Should().Be(actual.Name);
        expected.Description.Should().Be(actual.Description);
        expected.CompressedData.Should().BeEquivalentTo(actual.CompressedData);
        expected.Models.Select(m => m.Id).Should().BeEquivalentTo(actual.Models.Select(m => m.Id));
    }
}
