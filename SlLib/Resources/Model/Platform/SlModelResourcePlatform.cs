using System.Text.Json.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Platform;

public class SlModelResourcePlatform : IResourceSerializable
{
    /// <summary>
    ///     The model resource this platform data belongs to.
    /// </summary>
    [JsonIgnore] public SlModelResource? Resource;
    
    /// <summary>
    ///     Vertex declarations used by this model.
    /// </summary>
    public List<SlVertexDeclaration> Declarations = [];
    
    /// <summary>
    ///     Vertex streams used by this model.
    /// </summary>
    public List<SlStream> VertexStreams = [];
    
    /// <summary>
    ///     Primary index stream used by this model.
    /// </summary>
    public SlStream IndexStream = new();
    
    /// <summary>
    ///     Extra index stream, unsure what it's used for, maybe something with shapes?
    /// </summary>
    public SlStream? ExtraIndexStream;

    public virtual void Load(ResourceLoadContext context)
    {
        Resource = context.LoadPointer<SlModelResource>();

        int vertexStreamData, vertexDeclData;
        int numVertexStreams, numVertexDeclarations;

        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            vertexStreamData = context.ReadPointer();
            vertexDeclData = context.ReadPointer();
            IndexStream = context.LoadPointer<SlStream>()!;
            ExtraIndexStream = context.LoadPointer<SlStream>();
            numVertexStreams = context.ReadInt32();
            numVertexDeclarations = context.ReadInt32();
        }
        else
        {
            numVertexStreams = context.ReadInt32();
            vertexStreamData = context.ReadPointer();
            numVertexDeclarations = context.ReadInt32();
            vertexDeclData = context.ReadPointer();
            IndexStream = context.LoadPointer<SlStream>()!;
            ExtraIndexStream = context.LoadPointer<SlStream>();
        }

        VertexStreams = context.LoadArray<SlStream>(vertexStreamData, numVertexStreams);
        Declarations = context.LoadArray<SlVertexDeclaration>(vertexDeclData, numVertexDeclarations);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x30 : 0x1c;
    }
}