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
    ///     The scene this chunk is contained in.
    /// </summary>
    public string Scene = string.Empty;

    /// <summary>
    ///     The relocation descriptors for the CPU data.
    /// </summary>
    public List<SlResourceRelocation> Relocations = [];

    public SlResourceChunk(SlResourceType type, SlPlatform platform, int version, byte[] data, byte[] gpu,
        bool isResource)
    {
        Type = type;
        Version = version;
        Data = data;
        GpuData = gpu;
        IsResource = isResource;
        
        // Cache the name and ID of the chunk from its header
        if (isResource)
        {
            Id = platform.ReadInt32(data.AsSpan(0, 4));
            // Header data got swapped around Android revision
            if (Version >= SlPlatform.Android.DefaultVersion)
            {
                int addr = platform.ReadInt32(data.AsSpan(8, 4));
                Name = Data.ReadString(addr);    
            }
            else
            {
                int addr = platform.ReadInt32(data.AsSpan(4, 4));
                Name = Data.ReadString(addr);    
            }
        }
        else
        {
            Id = platform.ReadInt32(data.AsSpan(20, 4));
            
            int namePointerAddress = 0x1c;
            if (version >= SlPlatform.Android.DefaultVersion)
                namePointerAddress = 0x20;
            
            int addr = platform.ReadInt32(data.AsSpan(namePointerAddress, 4));
            Name = Data.ReadString(addr);
        }
        
        // Not every resource is going to have a scene tag
        if (Name.Contains(':'))
        {
            string parent = Path.GetFileName(Name.Split(':')[0]);
            if (parent.EndsWith(".mb"))
                Scene = Path.GetFileNameWithoutExtension(parent);
        }
    }
}