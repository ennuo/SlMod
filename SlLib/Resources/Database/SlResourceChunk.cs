namespace SlLib.Resources.Database;

public class SlResourceChunk
{
    /// <summary>
    ///     The CPU data associated with this chunk.
    /// </summary>
    public ArraySegment<byte> Data;

    /// <summary>
    ///     The GPU data associated with this chunk.
    /// </summary>
    public ArraySegment<byte> GpuData;

    /// <summary>
    ///     The unique identifier for this chunk.
    /// </summary>
    public int Id;

    /// <summary>
    ///     Whether this chunk is a node or a resource.
    /// </summary>
    public bool IsResource = false;

    /// <summary>
    ///     The name of this chunk.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The relocation descriptors for the CPU data.
    /// </summary>
    public List<SlResourceRelocation> Relocations = [];

    /// <summary>
    ///     The type of resource contained in this chunk.
    /// </summary>
    public SlResourceType Type = SlResourceType.Invalid;

    /// <summary>
    ///     The version of this chunk.
    /// </summary>
    public int Version = SlFileVersion.Windows;
}