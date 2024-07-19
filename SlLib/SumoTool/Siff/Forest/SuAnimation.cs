using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuAnimation : IResourceSerializable
{
    // 0 = SuSimpleAnimation
    // 1 = SuCubicAnimation16
    // 2 = SuCubicAnimation8
    // 3 = SuCubicAnimation16Reduced
    // 4 = SuCubicAnimation16Reduced2
    // 5 = ???
    // 6 = SuCubicAnimationReduced2Quantized<ushort, ushort>
    // 7 = SuCubicAnimationReduced2Quantized<byte, ushort>
    // 8 = SuCubicAnimationReduced2Quantized<ushort, float>
    // 9 = SuCubicAnimationReduced2Quantized<ushort, byte>
    // 10 = SuCubicAnimationReduced2Quantized<byte, byte>
    
    // most common type seems to be 6
    public int Type;
    public int NumFrames;
    public int NumBones;
    public int NumUvBones;
    public int NumFloatStreams;
    
    // Cubic animation shared
    public List<int> ChannelMasks = [];
    
    
    // structure for this is technically
        // short NumFrames
        // short NumKeys
            // short Frame[NumKeys]
    public List<short> AnimStreams = [];
    
    
    // Type 1
    public List<int> VectorOffsets = [];
    public List<Vector4> VectorFrameData = [];
    
    // Typ 4
    public List<float> FloatFrameData = [];
    
    // Type 6
    public List<short> FrameData = [];
    
    public void Load(ResourceLoadContext context)
    {
        int start = context.Position;


        Type = context.ReadInt32(); // 0x0
        NumFrames = context.ReadInt32(); // 0x4
        NumBones = context.ReadInt32(); // 0x8
        NumUvBones = context.ReadInt32(); // 0xc
        NumFloatStreams = context.ReadInt32(); // 0x10
        
        // bones
            // each translation is 0x12 bytes?
                // size += (key_data[1] * 0x12) + 0x11 & 0xfffffffc
            // each rotation is 0x18 bytes?
                // size += (key_data[1] * 0x18) + 0x13 & 0xfffffffc
            // each scale is 0x12 bytes?
                // // size += (key_data[1] * 0x12) + 0x11 & 0xfffffffc
        // uv bones
            // each translation is 0xc bytes?
                // size += (key_data[1] * 0xc) + 0xf & 0xfffffffc
            // each rotation is 0x6 bytes?
                // size += (key_data[1] * 0x6) + 0xd & 0xfffffffc
            // each scale is 0xc bytes?
                // size += (key_data[1] * 0xc) + 0xf & 0xfffffffc
        // float streams
            // size += (key_data[1] * 0x6) + 0xd & 0xfffffffc
            
        if (Type == 6)
        {
            int bits = NumFloatStreams + 0x1f + (NumUvBones + NumBones) * 4;
            int numDoubleWords = ((bits + (bits >> 0x1f & 0x1f)) >> 5);
            int wordDataOffset = 0x18 + (numDoubleWords * 4);
            int paramData = context.ReadInt32();
            
            for (int i = 0; i < numDoubleWords; ++i)
                ChannelMasks.Add(context.ReadInt32());

            int dataSize = 0;
            int maskIndex = 0;
            int headerStart = start + wordDataOffset;
            int headerOffset = headerStart;
            int mask = ChannelMasks[maskIndex++];
            int remaining = 8;
            
            for (int i = 0; i < NumBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x12 + 0x11 & ~3;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x18 + 0x13 & ~3;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x12 + 0x11 & ~3;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 8) != 0)
                {
                    int count = context.ReadInt16(headerOffset + 2);
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            for (int i = 0; i < NumUvBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0xc + 0xf & ~3;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x18 + 0x13 & ~3;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x6 + 0xd & ~3;
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            remaining <<= 4;
            for (int i = 0; i < NumFloatStreams; ++i)
            {
                if ((mask & 1) != 0)
                {
                    if (context.Platform.IsBigEndian)
                    {
                        var span = context._data.AsSpan(paramData + dataSize, sizeof(float) * 2);
                        var cast = MemoryMarshal.Cast<byte, int>(span);
                        BinaryPrimitives.ReverseEndianness(cast, cast);
                        var cast2 = MemoryMarshal.Cast<byte, short>(span);
                        BinaryPrimitives.ReverseEndianness(cast2, cast2);
                    }
                    
                    
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + count * 0x6 + 0xd & ~3;
                    headerOffset += (4 + (count * 2));
                }

                mask >>>= 1;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 32;
                }
            }
            
            int numFrameHeaders = (headerOffset - headerStart) / 0x2;
            int numPackedShorts = dataSize / 0x2;
            
            for (int i = 0; i < numFrameHeaders; ++i) AnimStreams.Add(context.ReadInt16());
            for (int i = 0; i < numPackedShorts; ++i) FrameData.Add(context.ReadInt16(paramData + (i * 2)));
        }
        else if (Type == 1)
        {
            int paramData = context.ReadInt32();
            for (int i = 0; i < NumBones * 2; ++i)
                VectorOffsets.Add(context.ReadInt32());
            
            int bits = NumFloatStreams + 0x1f + (NumUvBones + NumBones) * 4;
            int numDoubleWords = ((bits + (bits >> 0x1f & 0x1f)) >> 5);
            int wordDataOffset = 0x18 + (NumBones * 8) + (numDoubleWords * 4);
            
            for (int i = 0; i < numDoubleWords; ++i)
                ChannelMasks.Add(context.ReadInt32());

            int dataSize = 0;
            int maskIndex = 0;
            int headerStart = start + wordDataOffset;
            int headerOffset = headerStart;
            int mask = ChannelMasks[maskIndex++];
            int remaining = 8;
            
            for (int i = 0; i < NumBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x30;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x40;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x30;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 8) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            for (int i = 0; i < NumUvBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x20;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x10;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x20;
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            remaining <<= 4;
            for (int i = 0; i < NumFloatStreams; ++i)
            {
                if ((mask & 1) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize += count * 0x10;
                    headerOffset += (4 + (count * 2));
                }

                mask >>>= 1;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 32;
                }
            }
            
            int numFrameHeaders = (headerOffset - headerStart) / 0x2;
            int numPackedFloats = (dataSize / 0x10);
            
            for (int i = 0; i < numFrameHeaders; ++i) AnimStreams.Add(context.ReadInt16());
            for (int i = 0; i < numPackedFloats; ++i) VectorFrameData.Add(context.ReadFloat4(paramData + (i * 0x10)));
        }
        else if (Type == 4)
        { 
            int bits = NumFloatStreams + 0x1f + (NumUvBones + NumBones) * 4;
            int numDoubleWords = ((bits + (bits >> 0x1f & 0x1f)) >> 5);
            int wordDataOffset = 0x18 + (numDoubleWords * 4);
            int paramData = context.ReadInt32();
            
            for (int i = 0; i < numDoubleWords; ++i)
                ChannelMasks.Add(context.ReadInt32());

            int dataSize = 0;
            int maskIndex = 0;
            int headerStart = start + wordDataOffset;
            int headerOffset = headerStart;
            int mask = ChannelMasks[maskIndex++];
            int remaining = 8;
            
            for (int i = 0; i < NumBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0xc + count * 0x24;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0x10 + count * 0x30;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    int count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0xc + count * 0x24;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 8) != 0)
                {
                    int count = context.ReadInt16(headerOffset + 2);
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            for (int i = 0; i < NumUvBones; ++i)
            {
                if ((mask & 1) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0x8 + count * 0x18;
                    headerOffset += (4 + (count * 2));
                }
                
                if ((mask & 2) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0x4 + count * 0xc;
                    headerOffset += (4 + (count * 2));
                }

                if ((mask & 4) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0x8 + count * 0x18;
                    headerOffset += (4 + (count * 2));
                }
                
                mask >>>= 4;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 8;
                }
            }
            
            remaining <<= 4;
            for (int i = 0; i < NumFloatStreams; ++i)
            {
                if ((mask & 1) != 0)
                {
                    short count = context.ReadInt16(headerOffset + 2);
                    dataSize = dataSize + 0x4 + count * 0xc;
                    headerOffset += (4 + (count * 2));
                }

                mask >>>= 1;
                remaining -= 1;
                if (remaining == 0)
                {
                    mask = maskIndex < ChannelMasks.Count ? ChannelMasks[maskIndex++] : 0;
                    remaining = 32;
                }
            }
            
            int numFrameHeaders = (headerOffset - headerStart) / 0x2;
            int numPackedFloats = dataSize / 0x4;
            
            for (int i = 0; i < numFrameHeaders; ++i) AnimStreams.Add(context.ReadInt16());
            for (int i = 0; i < numPackedFloats; ++i) FloatFrameData.Add(context.ReadFloat(paramData + (i * 4)));
            
            // Console.WriteLine($"Unsupported animation type! " + Type + " : " + $"{(context.Position):x8}");
            // Console.WriteLine($"DataSize: {dataSize:x8}");
        }
        else
        {
            Console.WriteLine($"Unsupported animation type! " + Type + " : " + $"{(context.Position + context._data.Offset):x8}");
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Type, 0x0);
        context.WriteInt32(buffer, NumFrames, 0x4);
        context.WriteInt32(buffer, NumBones, 0x8);
        context.WriteInt32(buffer, NumUvBones, 0xc);
        context.WriteInt32(buffer, NumFloatStreams, 0x10);

        if (Type == 1)
        {
            ISaveBuffer frameData = context.SaveGenericPointer(buffer, 0x14, (VectorFrameData.Count * 0x10), align: 0x10);

            int offset = 0x18;
            
            foreach (int vo in VectorOffsets)
            {
                context.WriteInt32(buffer, vo, offset);
                offset += 4;
            }
            
            foreach (int mask in ChannelMasks)
            {
                context.WriteInt32(buffer, mask, offset);
                offset += 4;
            }

            foreach (short header in AnimStreams)
            {
                context.WriteInt16(buffer, header, offset);
                offset += 2;
            }
            
            for (int i = 0; i < VectorFrameData.Count; ++i)
                context.WriteFloat4(frameData, VectorFrameData[i], i * 0x10);
        }
        else if (Type == 4)
        {
            ISaveBuffer frameData = context.SaveGenericPointer(buffer, 0x14, (FloatFrameData.Count * 4), align: 0x10);
            
            int offset = 0x18;
            foreach (int mask in ChannelMasks)
            {
                context.WriteInt32(buffer, mask, offset);
                offset += 4;
            }

            foreach (short header in AnimStreams)
            {
                context.WriteInt16(buffer, header, offset);
                offset += 2;
            }
            
            for (int i = 0; i < FloatFrameData.Count; ++i)
                context.WriteFloat(frameData, FloatFrameData[i], (i * 0x4));
        }
        else if (Type == 6)
        {
            ISaveBuffer frameData = context.SaveGenericPointer(buffer, 0x14, (FrameData.Count * 2), align: 0x10);

            int offset = 0x18;
            foreach (int mask in ChannelMasks)
            {
                context.WriteInt32(buffer, mask, offset);
                offset += 4;
            }

            foreach (short header in AnimStreams)
            {
                context.WriteInt16(buffer, header, offset);
                offset += 2;
            }
            
            for (int i = 0; i < FrameData.Count; ++i)
                context.WriteInt16(frameData, FrameData[i], (i * 0x2));
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        int size = 20;
        if (Type == 1)
            size += 0x4 + (AnimStreams.Count * 0x2) + (ChannelMasks.Count * 0x4) + (NumBones * 8);
        if (Type is 6 or 4)
            size += 0x4 + (AnimStreams.Count * 0x2) + (ChannelMasks.Count * 0x4);
        return size;
    }
}