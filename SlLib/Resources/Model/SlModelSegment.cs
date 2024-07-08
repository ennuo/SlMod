using System.Text.Json.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlModelSegment : IResourceSerializable
{
    /// <summary>
    ///     The type of primitive to be rendered.
    /// </summary>
    public SlPrimitiveType PrimitiveType = SlPrimitiveType.Triangles;
    
    /// <summary>
    ///     The index of the material used by this segment.
    /// </summary>
    public int MaterialIndex;
    
    /// <summary>
    ///     The first vertex of this segment's primitive in the vertex stream.
    /// </summary>
    public int VertexStart;
    
    /// <summary>
    ///     The first index of this segment's primitive in the index buffer.
    /// </summary>
    public int FirstIndex;
    
    /// <summary>
    ///     Information about the sectors of this mesh segment.
    ///     <remarks>
    ///         Might be used for LOD meshes or shape keys?
    ///         First sector is always the main primitive.
    ///     </remarks>
    /// </summary>
    public List<SlModelSector> Sectors = [];
    
    /// <summary>
    ///     Convenience accessor for the primary sector.
    /// </summary>
    public SlModelSector Sector => Sectors[0];

    /// <summary>
    ///     The vertex declaration used by this segment.
    /// </summary>
    public SlVertexDeclaration Format = new();
    
    /// <summary>
    ///     The vertex streams used by this segment.
    /// </summary>
    public SlStream?[] VertexStreams = [null, null, null];

    /// <summary>
    ///     The index stream used by this segment.
    /// </summary>
    public SlStream IndexStream = new();
    
    /// <summary>
    ///     The weight stream used by this segment.
    /// </summary>
    [JsonIgnore] public ArraySegment<byte> WeightBuffer;
    
    /// <summary>
    ///     The joint stream used by this segment.
    /// </summary>
    [JsonIgnore] public ArraySegment<byte> JointBuffer;

    /// <summary>
    ///     Vertex array object used for OpenGL rendering
    /// </summary>
    public int VAO;
    
    /// <inheritdoc />
    public virtual void Load(ResourceLoadContext context)
    {
        int sectorData, numSectors = 0;
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            sectorData = context.ReadPointer();
        }
        else
        {
            ReadVertexInfo();
            numSectors = context.ReadInt32();
            sectorData = context.ReadPointer();
        }
        
        Format = context.LoadPointer<SlVertexDeclaration>()!;
        for (int i = 0; i < 3; ++i)
            VertexStreams[i] = context.LoadPointer<SlStream>();
        IndexStream = context.LoadPointer<SlStream>()!;
        
        // Can't read weight/joint data until we've read the sectors, so just read the pointers for now.
        int weightData = context.ReadPointer(out bool isWeightDataFromGpu);
        int jointData = context.ReadPointer(out bool isJointDataFromGpu);
        context.ReadPointer(); // Blendshape data
        if (context.Version <= 0x13) context.ReadPointer();
        
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            ReadVertexInfo();
            numSectors = context.ReadInt32();
            context.ReadInt32(); // ???
        }

        Sectors = context.LoadArray<SlModelSector>(sectorData, numSectors);
        
        int vertexSize = Sector.NumVerts * 0x10;
        if (weightData != 0)
            WeightBuffer = context.LoadBuffer(weightData, vertexSize, isWeightDataFromGpu);
        if (jointData != 0)
            JointBuffer = context.LoadBuffer(jointData, vertexSize, isJointDataFromGpu);
        
        return;
        
        void ReadVertexInfo()
        {
            PrimitiveType = (SlPrimitiveType)context.ReadInt32();
            if (context.Platform != SlPlatform.WiiU)
                MaterialIndex = context.ReadInt32();
            VertexStart = context.ReadInt32();
            FirstIndex = context.ReadInt32();   
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // This resource gets saved in the SlModel serialization function,
        // not here, it's weird, but it's just to preserve order of binaries
        throw new NotSupportedException();
    }
    
    public virtual int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform.Is64Bit) return 0x60;
        
        // Special cases for older Wii U data
        if (platform == SlPlatform.WiiU)
        {
            return version <= 0xb ? 0x38 : 0x44;
        }

        if (platform == SlPlatform.Xbox360) return 0x40;
        
        return version <= 0x1b ? 0x3c : 0x38;
    }
}