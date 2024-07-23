using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlModelSector : IResourceSerializable
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
    
    /// <summary>
    ///     The center of the bounding box for this sector.
    /// </summary>
    public Vector3 Center = Vector3.Zero;
    
    /// <summary>
    ///     The extents of the bounding box for this sector.
    /// </summary>
    public Vector3 Extents = new(0.5f, 0.5f, 0.5f);

    /// <summary>
    ///     The vertex offset of this sector.
    /// </summary>
    public int VertexOffset;
    
    public int Unknown = -1;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        Center = context.ReadFloat3();
        Extents = context.ReadFloat3();
        ElementOffset = context.ReadInt32();
        NumElements = context.ReadInt32();
        VertexOffset = context.ReadInt32();
        
        NumVerts = context.ReadInt32();
        // 0-based offset in earlier versions?
        if (context.Version <= 0x1b) NumVerts += 1;
        
        // 0x0 = min vert
        // 0x0 = min index
        // 0xd8 = indices
        // 0x8f = verts
        
        
        // 0x90 = min vert
        // 0xe0 = min index
        // 0x4d4 = indices
        // 0x337 = vertis
        
        
        
        
        // 0x03c8
        
        Unknown = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, Center, 0x0);
        context.WriteFloat3(buffer, Extents, 0xc);
        context.WriteInt32(buffer, ElementOffset, 0x18);
        context.WriteInt32(buffer, NumElements, 0x1c);
        context.WriteInt32(buffer, VertexOffset, 0x20);
        context.WriteInt32(buffer, NumVerts, 0x24);
        context.WriteInt32(buffer, Unknown, 0x28);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x2c;
    }
}