using SlLib.Extensions;

namespace SlLib.Resources.Database;

public class SlResourceChunk
{
    /// <summary>
    ///     The type of resource contained in this chunk.
    /// </summary>
    public SlResourceType Type;

    /// <summary>
    ///     The version of this chunk.
    /// </summary>
    public int Version;

    /// <summary>
    ///     Whether this chunk is a node or a resource.
    /// </summary>
    public bool IsResource;

    /// <summary>
    ///     The CPU data associated with this chunk.
    /// </summary>
    public byte[] Data;

    /// <summary>
    ///     The GPU data associated with this chunk.
    /// </summary>
    public byte[] GpuData;

    /// <summary>
    ///     The unique identifier for this chunk.
    /// </summary>
    public int Id;

    /// <summary>
    ///     The name of this chunk.
    /// </summary>
    public string Name;

    /// <summary>
    ///     The relocation descriptors for the CPU data.
    /// </summary>
    public List<SlResourceRelocation> Relocations = [];

    public SlResourceChunk(SlResourceType type, int version, byte[] data, byte[] gpu, bool isResource)
    {
        Type = type;
        Version = version;
        Data = data;
        GpuData = gpu;
        IsResource = isResource;

        // Cache the name and ID of the chunk from its header
        if (isResource)
        {
            Id = Data.ReadInt32(0);
            int addr = Data.ReadInt32(4);
            Name = Data.ReadString(addr);
        }
        else
        {
            Id = Data.ReadInt32(20);
            int addr = Data.ReadInt32(28);
            Name = Data.ReadString(addr);
        }
    }
}