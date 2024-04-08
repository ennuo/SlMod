namespace SlLib.Serialization;

/// <summary>
///     Represents a "slab" of memory.
/// </summary>
public class Slab : ISaveBuffer
{
    /// <summary>
    ///     Whether nor not this slab is stored in GPU data.
    /// </summary>
    public readonly bool IsGpuData;

    /// <summary>
    ///     The next slab in the list.
    /// </summary>
    public Slab? Next;

    /// <summary>
    ///     The previous slab in the list.
    /// </summary>
    public Slab? Previous;

    /// <summary>
    ///     Constructs and appends a new slab to the list.
    /// </summary>
    /// <param name="previous">The previous slab in the list</param>
    /// <param name="address">The byte offset of this slab</param>
    /// <param name="size">The size of this slab</param>
    /// <param name="isGpuData">Whether or not this slab is stored in GPU data</param>
    public Slab(Slab? previous, int address, int size, bool isGpuData)
    {
        Address = address;
        Previous = previous;
        Next = null;
        Data = new byte[size];
        IsGpuData = isGpuData;
    }

    /// <summary>
    ///     The byte offset of this slab.
    /// </summary>
    public int Address { get; }

    /// <summary>
    ///     The data held by this slab.
    /// </summary>
    public ArraySegment<byte> Data { get; }

    /// <inheritdoc />
    public ISaveBuffer At(int offset, int size)
    {
        return new Crumb(this, offset, size);
    }
}