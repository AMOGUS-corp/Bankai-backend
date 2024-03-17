using Bankai.MLApi.Infrastructure.Extensions;
using TypeExtensions=Bankai.MLApi.Infrastructure.Extensions.TypeExtensions;

namespace Bankai.MLApi.Tests.Infrastructure.Extensions;

[TestSubject(typeof(TypeExtensions))]
public class TypeExtensionsTest : MLApiTestsBase
{
    [Theory]
    [InlineData(typeof(IEnumerable<string>), typeof(IEnumerable<>), true)]
    [InlineData(typeof(IDictionary<string, int>), typeof(IEnumerable<>), false)]
    public void IsTypeOfGenericTypeTest(Type type, Type genericType, bool equals) =>
        type.IsTypeOfGenericType(genericType).Should().Be(equals);
}
