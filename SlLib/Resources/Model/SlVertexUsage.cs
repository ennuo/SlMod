namespace SlLib.Resources.Model;

/// <summary>
///     The usage options for how a resource is used in a stream.
/// </summary>
public static class SlVertexUsage
{
    public const int Position = 0;
    public const int Normal = 1;
    public const int Color = 2;
    public const int TextureCoordinate = 3;
    public const int Tangent = 4;

    // Not sure if these are what these actually are;
    // they're not used anywhere anyway; just using them
    // for loading legacy models
    public const int BiNormal = 5;
    public const int BlendWeight = 6;
    public const int BlendIndices = 7;

    public const int Count = 8;
}