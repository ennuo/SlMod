using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Shared stream object for index and vertex buffers.
/// </summary>
public class SlStream : IResourceSerializable
{
    /// <summary>
    ///     The number of elements in this stream.
    /// </summary>
    public int Count;

    /// <summary>
    ///     Data held in this stream.
    /// </summary>
    [JsonIgnore] public ArraySegment<byte> Data;

    /// <summary>
    ///     Whether or not the buffer should be stored on the GPU.
    /// </summary>
    public bool Gpu = true;

    /// <summary>
    ///     The size of each element in this stream.
    /// </summary>
    public int Stride;

    /// <summary>
    ///     Whether or not the stream is currently in big endian.
    /// </summary>
    public bool IsBigEndian = !BitConverter.IsLittleEndian;
    
    /// <summary>
    ///     VBO ID used for OpenGL rendering
    /// </summary>
    public int VBO;
    
    /// <summary>
    ///     Constructs an empty stream.
    /// </summary>
    public SlStream()
    {
    }

    /// <summary>
    ///     Allocates an empty stream with a set number of elements.
    /// </summary>
    /// <param name="count">The number of elements</param>
    /// <param name="stride">The stride of each element</param>
    public SlStream(int count, int stride)
    {
        Count = count;
        Stride = stride;
        Data = new byte[Count * Stride];
    }

    /// <summary>
    ///     Swaps the endianness of an index stream.
    /// </summary>
    public void SwapEndianness16()
    {
        var span = MemoryMarshal.Cast<byte, short>(Data);
        BinaryPrimitives.ReverseEndianness(span, span);
        IsBigEndian = !IsBigEndian;
    }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        IsBigEndian = context.Platform.IsBigEndian;

        context.Position += (0x8 + context.Platform.GetPointerSize());
        Count = context.ReadInt32();
        Stride = context.ReadInt32();
        
        context.Position += context.Platform.GetPointerSize(); // Platform -> Self pointer
        
        // Haven't actually confirmed if this is only Win64, or just a TSR thing yet
        if (context.Platform == SlPlatform.Win64) 
            context.Position += context.Platform.GetPointerSize();
        
        Data = context.LoadBufferPointer(Count * Stride, out Gpu);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, 0x1, 0x8); // Always 1?

        context.WriteInt32(buffer, Count, 0xc);
        context.WriteInt32(buffer, Stride, 0x10);

        context.SavePointer(buffer, this, 0x14);
        context.SaveBufferPointer(buffer, Data, 0x18, gpu: Gpu);

        context.WriteInt32(buffer, 0x10, 0x1c); // Always 16?
    }

    /// <inheritdoc />
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform == SlPlatform.WiiU) 
            return version > 0xb ? 0x34 : 0x28;
        if (platform == SlPlatform.Xbox360)
            return 0x40;
        return platform.Is64Bit ? 0x38 : 0x2c;
    }
}