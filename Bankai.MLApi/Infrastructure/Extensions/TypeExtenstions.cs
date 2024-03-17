namespace Bankai.MLApi.Infrastructure.Extensions;

public static class TypeExtensions
{
    public static bool IsTypeOfGenericType(this Type type, Type genericTypeDefinition)
        => type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
}
