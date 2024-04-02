using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlModelSegment : ILoadable
{
    /// <summary>
    ///     The first index of this segment's primitive in the index buffer.
    /// </summary>
    public int FirstIndex;

    /// <summary>
    ///     The vertex declaration used by this segment.
    /// </summary>
    public SlVertexDeclaration Format = new();

    /// <summary>
    ///     The index stream used by this segment.
    /// </summary>
    public SlStream IndexStream = new();

    /// <summary>
    ///     The joint stream used by this segment.
    /// </summary>
    public ArraySegment<byte> JointBuffer;

    /// <summary>
    ///     The index of the material used by this segment.
    /// </summary>
    public int MaterialIndex;

    /// <summary>
    ///     The type of primitive to be rendered.
    /// </summary>
    public SlPrimitiveType PrimitiveType = SlPrimitiveType.Triangles;

    /// <summary>
    ///     Information about the sectors of this mesh segment.
    ///     <remarks>
    ///         Might be used for LOD meshes or shape keys?
    ///         First sector is always the main primitive.
    ///     </remarks>
    /// </summary>
    public List<SlModelSector> Sectors = [];

    /// <summary>
    ///     The first vertex of this segment's primitive in the vertex stream.
    /// </summary>
    public int VertexStart;

    /// <summary>
    ///     The vertex streams used by this segment.
    /// </summary>
    public SlStream?[] VertexStreams = [null, null, null];

    /// <summary>
    ///     The weight stream used by this segment.
    /// </summary>
    public ArraySegment<byte> WeightBuffer;

    /// <summary>
    ///     Convenience accessor for the primary sector.
    /// </summary>
    public SlModelSector Sector => Sectors[0];

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        PrimitiveType = (SlPrimitiveType)context.ReadInt32(offset);
        MaterialIndex = context.ReadInt32(offset + 4);
        VertexStart = context.ReadInt32(offset + 8);
        FirstIndex = context.ReadInt32(offset + 12);

        int numSectors = context.ReadInt32(offset + 16);
        int sectorData = context.ReadInt32(offset + 20);
        for (int i = 0; i < numSectors; ++i)
            Sectors.Add(context.LoadReference<SlModelSector>(sectorData + i * 0x2c));

        Format = context.LoadPointer<SlVertexDeclaration>(offset + 24)!;
        for (int i = 0; i < 3; ++i)
            VertexStreams[i] = context.LoadPointer<SlStream>(offset + 28 + i * 4);

        IndexStream = context.LoadPointer<SlStream>(offset + 40)!;

        int vertexSize = Sector.NumVerts * 16;
        WeightBuffer = context.LoadBufferPointer(offset + 44, vertexSize);
        JointBuffer = context.LoadBufferPointer(offset + 48, vertexSize);

        // TODO: Add support for buffer @ (base + 0x34), it's something to do with either dynamic data or blendshapes?
    }
}