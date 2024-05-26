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
    public const int BiNormal = 5;
    public const int BlendWeight = 6;
    public const int BlendIndices = 7;
    public const int PointSize = 8;
    public const int Fog = 9;
    public const int AlsoTexCoord = 10;

    public const int Count = 11;
}