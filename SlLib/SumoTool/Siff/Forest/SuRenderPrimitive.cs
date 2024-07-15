using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderPrimitive : IResourceSerializable
{
    /// <summary>
    ///     Center transform for OBB of this primitive.
    /// </summary>
    public Matrix4x4 ObbTransform;

    /// <summary>
    ///     OBB extents containing this primitive.
    /// </summary>
    public Vector4 Extents;
    
    /// <summary>
    ///     Culling sphere containing this primitive.
    /// </summary>
    public Vector4 CullSphere;

    public byte[] TextureMatrixIndices = Enumerable.Repeat((byte)0xFF, 0x8).ToArray();
    public TextureUVMap[] TextureUvMaps = Enumerable.Repeat(new TextureUVMap(0xFF, 0xFF, 0xFF), 8).ToArray();
    public int textureUVMapHashVS;
    public int textureUVMapHashPS;
    
    
    /// <summary>
    ///     The material used to paint this primitive.
    /// </summary>
    public SuRenderMaterial? Material;
    
    /// <summary>
    ///     The number of indices referenced by this primitive.
    /// </summary>
    public int NumIndices;
    
    /// <summary>
    ///     Index data referenced by this primitive.
    /// </summary>
    public ArraySegment<byte> IndexData = ArraySegment<byte>.Empty;
    
    /// <summary>
    ///     Vertex stream used by this primitive.
    /// </summary>
    public SuRenderVertexStream? VertexStream;
    
    public void Load(ResourceLoadContext context)
    {
        ObbTransform = context.ReadMatrix();
        Extents = context.ReadFloat4();
        CullSphere = context.ReadFloat4();

        for (int i = 0; i < 8; ++i)
            TextureMatrixIndices[i] = context.ReadInt8();
        for (int i = 0; i < 8; ++i)
            TextureUvMaps[i] = new TextureUVMap(context.ReadInt8(), context.ReadInt8(), context.ReadInt8());
        textureUVMapHashVS = context.ReadInt32();
        textureUVMapHashPS = context.ReadInt32();
        
        Material = context.LoadPointer<SuRenderMaterial>();
        NumIndices = context.ReadInt32();

        // We don't actually have access to relocations, so can't use the standard nonsense
        // we use for transformed resources.
        IndexData = context.LoadBuffer(context.ReadPointer(), NumIndices * 0x2, true);
        
        // Target is always PC's format, swap the endianness if the loaded data is big endian.
        if (context.Platform.IsBigEndian)
        {
            var span = MemoryMarshal.Cast<byte, short>(IndexData);
            BinaryPrimitives.ReverseEndianness(span, span);
        }
        
        // haven't seen this value used yet, so...
        context.Position += 0x4;

        VertexStream = context.LoadPointer<SuRenderVertexStream>();
        context.ReadInt32(); // always 0xaa, something else? i dunno
        
        // only processes mesh data if this is > -1
        context.ReadInt32(); // 2? can also be 3? sometimes 1???
        
        // -1, supposedly 2 shorts, looks like it's always set to -1 on start, so can safely ignore
        context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteMatrix(buffer, ObbTransform, 0x0);
        context.WriteFloat4(buffer, Extents, 0x40);
        context.WriteFloat4(buffer, CullSphere, 0x50);
        for (int i = 0; i < 8; ++i)
        {
            context.WriteInt8(buffer, TextureMatrixIndices[i], 0x60 + i);
            context.WriteInt8(buffer, TextureUvMaps[i].In, 0x68 + (i * 3) + 0);
            context.WriteInt8(buffer, TextureUvMaps[i].Out, 0x68 + (i * 3) + 1);
            context.WriteInt8(buffer, TextureUvMaps[i].Mat, 0x68 + (i * 3) + 2);
        }
        
        context.WriteInt32(buffer, textureUVMapHashVS, 0x80);
        context.WriteInt32(buffer, textureUVMapHashPS, 0x84);
        
        context.SavePointer(buffer, Material, 0x88, align: 0x10);
        context.WriteInt32(buffer, NumIndices, 0x8c);
        context.SaveBufferPointer(buffer, IndexData, 0x90, align: 0x10, gpu: true);
        context.SavePointer(buffer, VertexStream, 0x98);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xa8;
    }
    
    public struct TextureUVMap(byte i, byte o, byte m)
    {
        public byte In =  i;
        public byte Out = o;
        public byte Mat = m;
    }
}