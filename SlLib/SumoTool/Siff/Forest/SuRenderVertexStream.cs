using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest.DirectX;
using SlLib.SumoTool.Siff.Forest.DirectX.Xbox;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderVertexStream : IResourceSerializable
{
    private const int MaxStreams = 2;
    
    public List<D3DVERTEXELEMENT9> AttributeStreamsInfo = [];
    public ArraySegment<byte> Stream = ArraySegment<byte>.Empty;
    public ArraySegment<byte> ExtraStream = ArraySegment<byte>.Empty;
    public int VertexStride;
    public int VertexCount;

    // Don't know what this is, but I figure I need to serialize it.
    public int ExtraStreamFlags;
    
    public void Load(ResourceLoadContext context)
    {
        // 0x0 = D3DVERTEXELEMENT9*
        // 0x4 = int = stream hash, seems computed at runtime, so can ignore
        // 0x8 = int = vertex flags? (1, 4) (apparently on wii 4 means gpu data)
        // 0xc = int = Vertex Stride
        // 0x10 = int = Vertex Count
        // 0x14 = byte* = Vertex Stream (GPU DATA)
        // 0x18 = ???
        // 0x1c = ???
        // 0x20 = ??? = pointer in cpu data
        // 0x24 = int = ??? (flags?)
        // 0x28 = ??? = pointer in cpu data
        // 0x2c = int = some count
        // 0x30 = ??? = pointer in cpu data
        // 0x34 = ??? = pointer in cpu data (based on count in 0x2c)
        
        // Xbox
        // 0x0 = VtxAttributeInfo*
        // 0x4 = ??? (0)
        // 0x8 = int = Vertex Stride
        // 0xc = int = Vertex Data Size
        // 0x10 = void* = Vertex Data
        // 0x14 = int = Num Extra Vertex Streams(?)
        // 0x18 = void* = Extra Vertex Stream
        
        int attributeData = context.ReadPointer();
        if (attributeData == 0) throw new SerializationException("Vertex stream descriptor attributes were NULL!");
        
        Span<int> streamSizes = stackalloc int[MaxStreams];
        streamSizes.Clear();
        
        if (context.Platform == SlPlatform.Win32)
        {
            int offset = attributeData;
            while (context.ReadInt16(offset) != 0xff)
            {
                var attribute = new D3DVERTEXELEMENT9
                {
                    Stream = context.ReadInt16(offset + 0),
                    Offset = context.ReadInt16(offset + 2),
                    Type = (D3DDECLTYPE)context.ReadInt8(offset + 4),
                    Method = (D3DDECLMETHOD)context.ReadInt8(offset + 5),
                    Usage = (D3DDECLUSAGE)context.ReadInt8(offset + 6),
                    UsageIndex = context.ReadInt8(offset + 7),
                };

                if (attribute.Stream >= MaxStreams)
                    throw new SerializationException(
                        "ERROR! Only 2 vertex streams max are supported per SuRenderVertexStream!");


                int size = attribute.Offset + D3DVERTEXELEMENT9.GetTypeSize(attribute.Type);
                streamSizes[attribute.Stream] = Math.Max(size, streamSizes[attribute.Stream]);
                
                AttributeStreamsInfo.Add(attribute);
                
                offset += 8;
            }
            
            context.Position += 0x4;
            ExtraStreamFlags = context.ReadInt32();
            
            VertexStride = context.ReadInt32();
            VertexCount = context.ReadInt32();
            Stream = context.LoadBuffer(context.ReadInt32(), VertexCount * VertexStride, true);
            
            context.Position += 0x8;
            
            int extraStreamData = context.ReadPointer();
            if (extraStreamData != 0)
                ExtraStream = context.LoadBuffer(extraStreamData, streamSizes[1] * VertexCount, false);
            
            context.Position += 4; // some count
            int addr1 = context.ReadPointer(); // pointer to some 0x8 byte structure?? i dunno
            // if (addr1 != 0)
            // {
            //     Console.WriteLine($"[0]=0x{streamSizes[0]:x8}, 1=0x{streamSizes[1]:x8}, 2=0x{streamSizes[2]:x8}");
            //     Console.WriteLine($"0x{(start):x8} (addr1=0x{(context._data.Offset + addr1):x8})");
            // }
        }
        // Have to switch how we handle this dependent on the platform,
        // I don't really have any interest in supporting any other platforms right now,
        // so we'll just convert data to PC format.
        else if (context.Platform == SlPlatform.Xbox360)
        {
            int offset = attributeData;
            
            while (context.ReadInt16(offset) != 0xff)
            {
                var attribute = new XboxVertexElement
                {
                    Stream = context.ReadInt16(offset + 0),
                    Offset = context.ReadInt16(offset + 2),
                    Type = (XboxDeclType)context.ReadInt32(offset + 4),
                    Method = (D3DDECLMETHOD)context.ReadInt8(offset + 8),
                    Usage = (D3DDECLUSAGE)context.ReadInt8(offset + 9),
                    UsageIndex = context.ReadInt8(offset + 10),
                };
                
                if (attribute.Stream >= MaxStreams)
                    throw new SerializationException(
                        "ERROR! Only 2 vertex streams max are supported per SuRenderVertexStream!");
                
                // Tangents might be an issue since they're DEC4N,
                // but we'll just ignore that for now since I think the game only actually uses
                // XYZ?
                
                // If it's too much of an issue, I'll write a routine to inject a new data type
                // into the stream, but for now this should be fine.
                AttributeStreamsInfo.Add(new D3DVERTEXELEMENT9
                {
                    Stream = attribute.Stream,
                    Offset = attribute.Offset,
                    Type = XboxVertexElement.MapTypeToD3D9(attribute.Type),
                    Method = attribute.Method,
                    Usage = attribute.Usage,
                    UsageIndex = attribute.UsageIndex
                });

                int size = attribute.Offset + XboxVertexElement.GetTypeSize(attribute.Type);
                streamSizes[attribute.Stream] = Math.Max(size, streamSizes[attribute.Stream]);
                
                offset += 12;
            }
            
            context.Position += 4;

            int vertexStride = context.ReadPointer();
            int vertexDataSize = context.ReadInt32();
            VertexCount = vertexDataSize / vertexStride;
            Stream = context.LoadBuffer(context.ReadInt32(), VertexCount * streamSizes[0], true);
            ExtraStreamFlags = context.ReadInt32(); // this is the same as that flags thing @ 0x8 on pc? guess they just moved it
            ExtraStream = context.LoadBuffer(context.ReadInt32(), VertexCount * streamSizes[1], false);
            
            // Swap the endianness of the vertex streams for PC
            List<ArraySegment<byte>> streams = [Stream, ExtraStream];
            foreach (D3DVERTEXELEMENT9 element in AttributeStreamsInfo)
            {
                // All of these elements use bytes, so endianness shouldn't matter.
                if (element.Type is D3DDECLTYPE.D3DCOLOR or D3DDECLTYPE.UBYTE4 or D3DDECLTYPE.UBYTE4N) continue;
                
                int streamSize = streamSizes[element.Stream];
                int elementSize = D3DVERTEXELEMENT9.GetTypeSize(element.Type);
                
                for (int i = 0; i < VertexCount; ++i)
                {
                    int elementOffset = i * streamSize + element.Offset;
                    var data = streams[element.Stream].AsSpan(elementOffset, elementSize);
                    switch (element.Type)
                    {
                        case D3DDECLTYPE.FLOAT1:
                        case D3DDECLTYPE.FLOAT2:
                        case D3DDECLTYPE.FLOAT3:
                        case D3DDECLTYPE.FLOAT4:
                        case D3DDECLTYPE.UDEC3: 
                        case D3DDECLTYPE.DEC3N:
                            var dwordSpan = MemoryMarshal.Cast<byte, int>(data);
                            BinaryPrimitives.ReverseEndianness(dwordSpan, dwordSpan);
                            break;
                        case D3DDECLTYPE.SHORT2:
                        case D3DDECLTYPE.SHORT4:
                        case D3DDECLTYPE.SHORT2N:
                        case D3DDECLTYPE.USHORT4N:
                        case D3DDECLTYPE.FLOAT16x2: 
                        case D3DDECLTYPE.FLOAT16x4:
                            var wordSpan = MemoryMarshal.Cast<byte, short>(data);
                            BinaryPrimitives.ReverseEndianness(wordSpan, wordSpan);
                            break;
                    }
                }
            }
            
            
            
            // PC
            // Position : FLOAT3
            // Normal : FLOAT16x4
            // Tangent : FLOAT16x4
            // TexCoord : FLOAT16x2
            // BlendWeight : UBYTE4N
            // BlendIndices : UBYTE4

            // XBOX
            // Position : FLOAT3
            // Normal : DEC3N
            // Tangent : DEC4N
            // TexCoord : FLOAT16x2
            // BlendWeight : UBYTE4N
            // BlendIndices : UBYTE4
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        ISaveBuffer attributeData = context.SaveGenericPointer(buffer, 0x0, (AttributeStreamsInfo.Count * 8) + 2);
        {
            int offset = 0;
            foreach (D3DVERTEXELEMENT9 element in AttributeStreamsInfo)
            {
                context.WriteInt16(attributeData, element.Stream, offset + 0);
                context.WriteInt16(attributeData, element.Offset, offset + 2);
                context.WriteInt8(attributeData, (byte)element.Type, offset + 4);
                context.WriteInt8(attributeData, (byte)element.Method, offset + 5);
                context.WriteInt8(attributeData, (byte)element.Usage, offset + 6);
                context.WriteInt8(attributeData, (byte)element.UsageIndex, offset + 7);
                
                offset += 8;
            }
            context.WriteInt16(attributeData, 0xFF, offset);   
        }
        
        context.WriteInt32(buffer, ExtraStreamFlags, 0x8);
        context.WriteInt32(buffer, VertexStride, 0xc);
        context.WriteInt32(buffer, VertexCount, 0x10);
        context.SaveBufferPointer(buffer, Stream, 0x14, align: 0x10, gpu: true);
        context.SaveBufferPointer(buffer, ExtraStream, 0x1c, align: 0x10, gpu: false);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform == SlPlatform.Xbox360 ? 0x50 : 0x38;
    }
}