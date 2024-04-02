using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Shared stream object for index and vertex buffers.
/// </summary>
public class SlStream : ILoadable
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
        Data = context.LoadBufferPointer(offset + 0x18, Count * Stride);
    }
}