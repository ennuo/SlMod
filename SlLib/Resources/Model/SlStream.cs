using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Shared stream object for index and vertex buffers.
/// </summary>
public class SlStream : ILoadable, IWritable
{
    /// <summary>
    ///     The number of elements in this stream.
    /// </summary>
    public int Count;

    /// <summary>
    ///     Data held in this stream.
    /// </summary>
    public ArraySegment<byte> Data;

    /// <summary>
    ///     Whether or not the buffer should be stored on the GPU.
    /// </summary>
    public bool Gpu = true;

    /// <summary>
    ///     The size of each element in this stream.
    /// </summary>
    public int Stride;

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

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Count = context.ReadInt32(offset + 0xc);
        Stride = context.ReadInt32(offset + 0x10);
        Data = context.LoadBufferPointer(offset + 0x18, Count * Stride, out Gpu);
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
    public int GetAllocatedSize()
    {
        return 0x2c;
    }
}