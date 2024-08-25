using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest.DirectX;
using SlLib.SumoTool.Siff.Forest.DirectX.Xbox;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderVertexStream : IResourceSerializable
{
    private const int MaxStreams = 2;
    
    public List<D3DVERTEXELEMENT9> AttributeStreamsInfo = [];
    public ArraySegment<byte> Stream = ArraySegment<byte>.Empty;
    public ArraySegment<byte> ExtraStream = ArraySegment<byte>.Empty;
    public int VertexStride;
    public int VertexCount;
    
    public int NumExtraStreams;
    public VertexStreamHashes? StreamHashes;
    
    public int VertexStreamFlags;
    
    public void Load(ResourceLoadContext context)
    {
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
            NumExtraStreams = context.ReadInt32();
            
            VertexStride = context.ReadInt32();
            VertexCount = context.ReadInt32();
            Stream = context.LoadBuffer(context.ReadInt32(), VertexCount * VertexStride, true);
            
            context.Position += 0x8;
            
            int extraStreamData = context.ReadPointer();
            VertexStreamFlags = context.ReadInt32();
            StreamHashes = context.LoadPointer<VertexStreamHashes>();
            if (StreamHashes != null) StreamHashes.NumStreams = NumExtraStreams;
            
            if (extraStreamData != 0)
            {
                int streamSize = VertexCount * (NumExtraStreams + 1) * 0xc;
                ExtraStream = context.LoadBuffer(extraStreamData, streamSize, false);   
            }
        }
        // Have to switch how we handle this dependent on the platform,
        // I don't really have any interest in supporting any other platforms right now,
        // so we'll just convert data to PC format.
        else if (context.Platform == SlPlatform.Xbox360)
        {
            int offset = attributeData;

            List<XboxVertexElement> xboxVertexElements = [];
            int mainStreamSize = 0;
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
                
                xboxVertexElements.Add(attribute);
                if (attribute.Stream >= MaxStreams)
                    throw new SerializationException(
                        "ERROR! Only 2 vertex streams max are supported per SuRenderVertexStream!");
                
                // NVIDIA GPUs don't support DEC3N/DEC4N, so we'll just
                // remap them to Float16x4.
                XboxDeclType type = attribute.Type;
                if (type is XboxDeclType.DEC3N or XboxDeclType.DEC4N)
                    type = XboxDeclType.FLOAT16x4;

                if (type is XboxDeclType.UDEC3N or XboxDeclType.UDEC4N)
                    throw new SerializationException("Unsupported!");

                // Only the first stream is actually mapped to the vertex data,
                // the rest of the streams are for morph data which doesn't
                // really follow this attribute map.
                // So since we're converting DEC3N/DEC4N, we'll need to remap the offsets
                int elementDataOffset = attribute.Offset;
                if (attribute.Stream == 0)
                {
                    elementDataOffset = mainStreamSize;
                    mainStreamSize += XboxVertexElement.GetTypeSize(type);
                }
                
                AttributeStreamsInfo.Add(new D3DVERTEXELEMENT9
                {
                    Stream = attribute.Stream,
                    Offset = (short)elementDataOffset,
                    Type = XboxVertexElement.MapTypeToD3D9(type),
                    Method = attribute.Method,
                    Usage = attribute.Usage,
                    UsageIndex = attribute.UsageIndex
                });
                
                int size = attribute.Offset + XboxVertexElement.GetTypeSize(attribute.Type);
                streamSizes[attribute.Stream] = Math.Max(size, streamSizes[attribute.Stream]);
                
                offset += 12;
            }
            
            context.Position += 4;
            
            VertexStride = context.ReadInt32();
            int vertexDataSize = context.ReadInt32();
            VertexCount = vertexDataSize / VertexStride;
            
            int streamDataOffset = context.ReadPointer();
            Stream = context.LoadBuffer(streamDataOffset, VertexCount * streamSizes[0], true);
            NumExtraStreams = context.ReadInt32(); // this is the same as that flags thing @ 0x8 on pc? guess they just moved it
            int extraStreamData = context.ReadPointer();
            // Seems to basically always be 00's?
            context.Position += 0x20;
            
            // Convert the flags to something more appropriate for Win32
            VertexStreamFlags = context.ReadInt32();
            VertexStreamFlags |= 1;
            VertexStreamFlags &= ~0x0400;
            
            // Dumb hack!
            StreamHashes = context.LoadPointer<VertexStreamHashes>();
            if (StreamHashes != null) StreamHashes.NumStreams = NumExtraStreams;
            
            if (extraStreamData != 0)
            {
                int streamSize = VertexCount * (NumExtraStreams + 0x1) * 0x8;
                ExtraStream = context.LoadBuffer(extraStreamData, streamSize, false);
                
                // need to rebuild the stream with floats
                byte[] extraStream = new byte[VertexCount * (NumExtraStreams + 0x1) * 0xc];
                int streamOffset = 0;
                var wordSpan = MemoryMarshal.Cast<byte, short>(ExtraStream);
                BinaryPrimitives.ReverseEndianness(wordSpan, wordSpan);
                for (int i = 0; i < wordSpan.Length; i += 4)
                {
                    float x = (float)BitConverter.Int16BitsToHalf(wordSpan[i]);
                    float y = (float)BitConverter.Int16BitsToHalf(wordSpan[i + 1]);
                    float z = (float)BitConverter.Int16BitsToHalf(wordSpan[i + 2]);
                    
                    extraStream.WriteFloat(x, streamOffset + 0x0);
                    extraStream.WriteFloat(y, streamOffset + 0x4);
                    extraStream.WriteFloat(z, streamOffset + 0x8);
                    
                    streamOffset += 0xc;
                }

                ExtraStream = extraStream;
            }

            byte[] stream = new byte[VertexCount * mainStreamSize];
            for (int i = 0; i < xboxVertexElements.Count; ++i)
            {
                XboxVertexElement xboxElement = xboxVertexElements[i];
                D3DVERTEXELEMENT9 winElement = AttributeStreamsInfo[i];
                if (xboxElement.Stream != 0) continue; // Stream 1 is already handled
                
                int xboxStreamSize = streamSizes[xboxElement.Stream];
                int winStreamSize = mainStreamSize;
                int xboxElementSize = XboxVertexElement.GetTypeSize(xboxElement.Type);
                int winElementSize = D3DVERTEXELEMENT9.GetTypeSize(winElement.Type);

                for (int j = 0; j < VertexCount; ++j)
                {
                    int xboxElementOffset = j * xboxStreamSize + xboxElement.Offset;
                    int winElementOffset = j * winStreamSize + winElement.Offset;
                    
                    var xboxSpan = Stream.AsSpan(xboxElementOffset, xboxElementSize);
                    var winSpan = stream.AsSpan(winElementOffset, winElementSize);
                    switch (winElement.Type)
                    {
                        case D3DDECLTYPE.D3DCOLOR:
                        case D3DDECLTYPE.UBYTE4N:
                        case D3DDECLTYPE.UBYTE4:
                            xboxSpan.CopyTo(winSpan);
                            break;
                        case D3DDECLTYPE.FLOAT1:
                        case D3DDECLTYPE.FLOAT2:
                        case D3DDECLTYPE.FLOAT3:
                        case D3DDECLTYPE.FLOAT4:
                            var xboxDwordSpan = MemoryMarshal.Cast<byte, int>(xboxSpan);
                            var winDwordSpan = MemoryMarshal.Cast<byte, int>(winSpan);
                            BinaryPrimitives.ReverseEndianness(xboxDwordSpan, winDwordSpan);
                            break;
                        case D3DDECLTYPE.SHORT2:
                        case D3DDECLTYPE.SHORT4:
                        case D3DDECLTYPE.SHORT2N:
                        case D3DDECLTYPE.USHORT4N:
                        case D3DDECLTYPE.FLOAT16x2: 
                        case D3DDECLTYPE.FLOAT16x4:
                            // If the source is a remapped data type, convert it to the correct format.
                            if (xboxElement.Type is XboxDeclType.DEC3N or XboxDeclType.DEC4N)
                            {
                                uint val = BinaryPrimitives.ReadUInt32BigEndian(xboxSpan);
                                float x = SlUtil.DenormalizeSigned10BitInt((ushort)((val >> 0) & 0x3FF));
                                float y = SlUtil.DenormalizeSigned10BitInt((ushort)((val >> 10) & 0x3FF));
                                float z = SlUtil.DenormalizeSigned10BitInt((ushort)((val >> 20) & 0x3FF));
                                float w = 1.0f;
                                if (xboxElement.Type == XboxDeclType.DEC4N)
                                    w = SlUtil.DenormalizeUnsigned3BitInt((byte)((val >> 30) & 0x3));

                                var halfSpan = MemoryMarshal.Cast<byte, Half>(winSpan);
                                halfSpan[0] = (Half)x;
                                halfSpan[1] = (Half)y;
                                halfSpan[2] = (Half)z;
                                halfSpan[3] = (Half)w;
                                
                                break;
                            }
                            
                            
                            var xboxWordSpan = MemoryMarshal.Cast<byte, short>(xboxSpan);
                            var winWordSpan = MemoryMarshal.Cast<byte, short>(winSpan);
                            BinaryPrimitives.ReverseEndianness(xboxWordSpan, winWordSpan);
                            
                            break;
                        default:
                            throw new SerializationException("Unsupported vertex element! " + winElement.Type);
                    }
                }
            }

            // Assign our remapped stream!
            Stream = stream;
            VertexStride = mainStreamSize;

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
        ISaveBuffer attributeData = context.SaveGenericPointer(buffer, 0x0, (AttributeStreamsInfo.Count * 8) + 8);
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
            
            // D3DVERTEXELEMENT9 terminator
            context.WriteInt32(attributeData, 0xFF, offset);   
            context.WriteInt32(attributeData, 0x11, offset + 4);
        }
        
        context.WriteInt32(buffer, NumExtraStreams, 0x8);
        context.WriteInt32(buffer, VertexStride, 0xc);
        context.WriteInt32(buffer, VertexCount, 0x10);
        context.SaveBufferPointer(buffer, Stream, 0x14, align: 0x10, gpu: true);
        context.SaveBufferPointer(buffer, ExtraStream, 0x20, align: 0x10, gpu: false);
        context.WriteInt32(buffer, VertexStreamFlags, 0x24);
        context.SavePointer(buffer, StreamHashes, 0x28, deferred: true);
    }
    
    public class VertexStreamHashes : IResourceSerializable
    {
        public int Flags;
        public int NumStreams;
        
        public void Load(ResourceLoadContext context)
        {
            Flags = context.ReadInt32();
        }

        public void Save(ResourceSaveContext context, ISaveBuffer buffer)
        {
            context.WriteInt32(buffer, Flags, 0x0);
        }
        
        public int GetSizeForSerialization(SlPlatform platform, int version)
        {
            return 0x4 + (NumStreams * 0x4);
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform == SlPlatform.Xbox360 ? 0x50 : 0x38;
    }
}