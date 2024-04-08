using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeNodeBase
{
    /// <summary>
    ///     Size of this class in memory.
    /// </summary>
    public int FileClassSize;

    /// <summary>
    ///     Node base flags.
    /// </summary>
    public int BaseFlags;

    /// <summary>
    ///     The unique identifier for this node.
    /// </summary>
    public int Uid;

    /// <summary>
    ///     The name of this node.
    /// </summary>
    public string UidName = string.Empty;

    /// <summary>
    ///     The tag of this node.
    /// </summary>
    public string Tag = string.Empty;

    /// <summary>
    ///     Whether or not the name of this node references a resource.
    /// </summary>
    public abstract bool NodeNameIsFilename { get; }

    /// <summary>
    ///     Gets the node name without the associated path.
    /// </summary>
    /// <returns>Node short name</returns>
    public string GetShortName()
    {
        if (string.IsNullOrEmpty(UidName)) return "NoName";
        int start = UidName.Length;
        while (start > 0)
        {
            char c = UidName[start - 1];
            if (c is '|' or '\\' or '/') break;
            start--;
        }

        return UidName[start..];
    }

    /// <summary>
    ///     Loads this node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected int LoadInternal(ResourceLoadContext context, int offset)
    {
        FileClassSize = context.ReadInt32(offset + 0x8);
        BaseFlags = context.ReadInt32(offset + 0xc);
        // offset + 0x10 is old flags, but it seems in serialization, they should always be the same.
        Uid = context.ReadInt32(offset + 0x14);
        UidName = context.ReadStringPointer(offset + 0x1c);
        // Fairly sure this is the tag pointer, not actually sure
        Tag = context.ReadStringPointer(offset + 0x24);

        return offset + 0x40;
    }
}