using Bankai.MLApi.Infrastructure.Extensions;

namespace Bankai.MLApi.Tests.Infrastructure.Extensions;

[TestSubject(typeof(ArrayExtensions))]
public class ArrayExtensionsTest : MLApiTestsBase
{
    [Fact]
    public void ToStreamTest()
    {
        var bytesCount = new Faker().Random.Int(0, 1000000);
        var bytes = new Faker().Random.Bytes(bytesCount);

        using var result = bytes.ToStream();

        result.Should().BeOfType<MemoryStream>();
        result.Length.Should().Be(bytesCount);

        var resBytes = (result as MemoryStream)!.ToArray();
        resBytes.ForEach((b, i) => b.Should().Be(bytes[i]));
    }
}
