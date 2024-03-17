using System.Collections.Immutable;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class ObjectExtensions
{
    public static T[] AsArray<T>(this T obj)
    {
        return new T[1]{ obj };
    }

    public static T Cast<T>(this object obj) => (T) obj;

    public static ImmutableArray<T> ToImmutableArray<T>(this T obj)
    {
        return ImmutableArray.Create<T>(obj.AsArray<T>());
    }

    public static TDestination To<TSource, TDestination>(
        this TSource source,
        Func<TSource, TDestination> func)
    {
        return func(source);
    }
}