namespace SlLib.Serialization;

/// <summary>
///     Generic buffer template for serialization
/// </summary>
public interface ISaveBuffer
{
    /// <summary>
    ///     The absolute byte offset of this buffer.
    /// </summary>
    public int Address { get; }

    /// <summary>
    ///     The data backing this buffer.
    /// </summary>
    public ArraySegment<byte> Data { get; }

    /// <summary>
    ///     Gets a slice of this buffer.
    /// </summary>
    /// <param name="offset">The offset into the buffer</param>
    /// <param name="size">The size of the data to slice</param>
    /// <returns>Slice of this buffer</returns>
    public ISaveBuffer At(int offset, int size);
}