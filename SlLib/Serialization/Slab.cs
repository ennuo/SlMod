namespace SlLib.Serialization;

/// <summary>
///     Represents a "slab" of memory.
/// </summary>
public class Slab
{
    /// <summary>
    ///     The byte offset of this slab.
    /// </summary>
    public readonly int Address;

    /// <summary>
    ///     The data held by this slab.
    /// </summary>
    public readonly ArraySegment<byte> Data;

    /// <summary>
    ///     Whether nor not this slab is stored in GPU data.
    /// </summary>
    public readonly bool IsGpuData;

    /// <summary>
    ///     The next slab in the list.
    /// </summary>
    public Slab Next;

    /// <summary>
    ///     The previous slab in the list.
    /// </summary>
    public Slab Previous;

    /// <summary>
    ///     Constructs and appends a new slab to the list.
    /// </summary>
    /// <param name="previous">The previous slab in the list</param>
    /// <param name="address">The byte offset of this slab</param>
    /// <param name="size">The size of this slab</param>
    /// <param name="isGpuData">Whether or not this slab is stored in GPU data</param>
    public Slab(Slab previous, int address, int size, bool isGpuData)
    {
        Address = address;
        Previous = previous;
        Next = this;
        Data = new byte[size];
        IsGpuData = isGpuData;
    }

    /// <summary>
    ///     Gets a slice of this slab.
    /// </summary>
    /// <param name="offset">The offset into the slab</param>
    /// <param name="size">The size of the data to slice</param>
    /// <returns>Slice of the slab</returns>
    public Crumb At(int offset, int size)
    {
        return new Crumb(this, offset, size);
    }
}