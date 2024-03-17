using System.Runtime.CompilerServices;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class EnumerableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (T obj in enumerable)
            action(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task ForEachAsync<T>(
        this IEnumerable<T> enumerable,
        Func<T, Task> funcAsync)
    {
        foreach (T obj in enumerable)
            await funcAsync(obj);
    }
}