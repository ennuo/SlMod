using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlModelSector : ILoadable, IWritable
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

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, V0, 0x0);
        context.WriteFloat3(buffer, V1, 0xc);
        context.WriteInt32(buffer, ElementOffset, 0x18);
        context.WriteInt32(buffer, NumElements, 0x1c);
        context.WriteInt32(buffer, VertexOffset, 0x20);
        context.WriteInt32(buffer, NumVerts, 0x24);
        context.WriteInt32(buffer, Unknown, 0x28);
    }

    public int GetAllocatedSize()
    {
        return 0x2c;
    }
}