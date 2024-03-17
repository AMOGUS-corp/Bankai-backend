namespace Bankai.MLApi.Common;

public static class PathConstants
{
    public static readonly string AppDllLocation = GetDirectoryName(GetExecutingAssembly().Location)!;
}