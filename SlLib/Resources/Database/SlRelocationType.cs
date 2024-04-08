namespace SlLib.Resources.Database;

public static class SlRelocationType
{
    public const int Pointer = 0;
    public const int Null = 1;
    public const int Resource = 2;

    // This is only ever used for constant buffers
    public const int ResourcePair = 3;

    public const int GpuPointer = 16;
}