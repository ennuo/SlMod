﻿namespace SlLib.Serialization;

/// <summary>
///     Represents a view into a slab.
/// </summary>
/// <param name="slab">The slab to create a slice of</param>
/// <param name="offset">The offset into the slab to start from</param>
/// <param name="size">The size of data to slice</param>
public class Crumb(Slab slab, int offset, int size) : ISaveBuffer
{
    /// <summary>
    ///     The address of this buffer view relative to the slab.
    /// </summary>
    public readonly int Offset = offset;

    /// <summary>
    ///     The slab that holds this buffer view.
    /// </summary>
    public readonly Slab Slab = slab;

    /// <summary>
    ///     The absolute address of this buffer view.
    /// </summary>
    public int Address => slab.Address + offset;

    /// <summary>
    ///     The buffer view.
    /// </summary>
    public ArraySegment<byte> Data { get; } = slab.Data[offset..(offset + size)];

    /// <inheritdoc />
    public ISaveBuffer At(int offset, int size)
    {
        return new Crumb(Slab, Offset + offset, size);
    }
}