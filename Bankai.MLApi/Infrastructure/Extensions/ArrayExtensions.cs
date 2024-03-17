namespace Bankai.MLApi.Infrastructure.Extensions;

public static class ArrayExtensions
{
    public static Stream ToStream(this byte[] @this) => new MemoryStream(@this);
}
