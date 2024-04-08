using System.Numerics;
using SlLib.Extensions;
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
        MaterialIndex = context.ReadInt32(offset + 0x4);
        VertexStart = context.ReadInt32(offset + 0x8);
        FirstIndex = context.ReadInt32(offset + 0xc);

        int numSectors = context.ReadInt32(offset + 0x10);
        int sectorData = context.ReadInt32(offset + 0x14);
        for (int i = 0; i < numSectors; ++i)
            Sectors.Add(context.LoadObject<SlModelSector>(sectorData + i * 0x2c));

        Format = context.LoadPointer<SlVertexDeclaration>(offset + 0x18)!;
        for (int i = 0; i < 3; ++i)
            VertexStreams[i] = context.LoadPointer<SlStream>(offset + 0x1c + i * 4);

        IndexStream = context.LoadPointer<SlStream>(offset + 0x28)!;

        int vertexSize = Sector.NumVerts * 0x10;
        WeightBuffer = context.LoadBufferPointer(offset + 0x2c, vertexSize, out _);
        JointBuffer = context.LoadBufferPointer(offset + 0x30, vertexSize, out _);

        if (context.ReadInt32(offset + 0x34) != 0)
        {
            // int32 NumMorphs
            // 

            // Console.WriteLine(context.ReadInt32(offset + 0x34));
            // Console.WriteLine(Sector.NumVerts);
            // Console.WriteLine(Sector.NumElements);
        }

        // TODO: Add support for buffer @ (base + 0x34), it's something to do with either dynamic data or blendshapes?
    }

    /// <summary>
    ///     Gets the indices in the primary sector of this segment.
    /// </summary>
    /// <returns>Index array</returns>
    public int[] GetIndices()
    {
        int[] indices = new int[Sector.NumElements];
        var buffer = IndexStream.Data;
        for (int i = FirstIndex * 2, j = 0; j < Sector.NumElements; i += 2, ++j)
            indices[j] = buffer.ReadInt16(i) & 0xffff;
        return indices;
    }

    public Vector4[] GetVertices(int usage, int index = 0)
    {
        return Format.Get(VertexStreams, usage, Sector.VertexOffset, Sector.NumVerts, index)!;
    }
}