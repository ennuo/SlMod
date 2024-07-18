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
    ///     The byte alignment of this slab.
    /// </summary>
    public int Align;

    /// <summary>
    ///     The previous slab in the list.
    /// </summary>
    public Slab? Previous;

    /// <summary>
    ///     The next slab in the list.
    /// </summary>
    public Slab? Next;

    /// <summary>
    ///     The parent of this slab.
    /// </summary>
    public Slab? Parent;
    
    /// <summary>
    ///     The next sibling of this slab.
    /// </summary>
    public Slab? NextSibling;

    /// <summary>
    ///     The first child of this slab.
    /// </summary>
    public Slab? FirstChild;
    
    /// <summary>
    ///     Constructs and appends a new slab to the list.
    /// </summary>
    /// <param name="previous">The previous slab in the list</param>
    /// <param name="parent">The parent of this slab in the list</param>
    /// <param name="address">The byte offset of this slab</param>
    /// <param name="align">The alignment of this slab</param>
    /// <param name="size">The size of this slab</param>
    /// <param name="isGpuData">Whether this slab is stored in GPU data</param>
    public Slab(Slab? previous, Slab? parent, int address, int size, int align, bool isGpuData)
    {
        if (parent != null)
        {
            Slab? lastChild = parent.FirstChild;
            while (lastChild is { NextSibling: not null })
                lastChild = lastChild.NextSibling;

            if (lastChild != null) lastChild.NextSibling = this;
            else parent.FirstChild = this;
        }

        Parent = parent;
        Align = align;
        Address = address;
        Data = new byte[size];
        Previous = previous;
        IsGpuData = isGpuData;
        if (Previous != null)
            Previous.Next = this;
    }

    /// <summary>
    ///     The byte offset of this slab.
    /// </summary>
    public int Address { get; set; }

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