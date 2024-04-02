using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlModelSector : ILoadable
{
    /// <summary>
    ///     The offset of the first element used by this sector.
    /// </summary>
    public int ElementOffset;

    /// <summary>
    ///     The number of elements in this sector.
    /// </summary>
    public int NumElements;

    /// <summary>
    ///     The number of vertices in this sector.
    /// </summary>
    public int NumVerts;

    public int Unknown = -1;
    public Vector3 V0;
    public Vector3 V1;

    /// <summary>
    ///     The vertex offset of this sector.
    /// </summary>
    public int VertexOffset;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        V0 = context.ReadFloat3(offset);
        V1 = context.ReadFloat3(offset + 0xc);
        ElementOffset = context.ReadInt32(offset + 0x18);
        NumElements = context.ReadInt32(offset + 0x1c);
        VertexOffset = context.ReadInt32(offset + 0x20);
        NumVerts = context.ReadInt32(offset + 0x24);
        Unknown = context.ReadInt32(offset + 0x28);
    }
}