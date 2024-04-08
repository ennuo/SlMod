using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeGraphNode : SeNodeBase
{
    /// <summary>
    ///     Unique identifier for the parent of this node.
    /// </summary>
    public int ParentUid;

    /// <summary>
    ///     Loads this graph node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        // So the actual structure is...
        // SePtr<SeGraphNode> Parent @ 0x0
        // SePtr<SeGraphNode> Child @ 0x8
        // SePtr<SeGraphNode> PrevSibling @ 0x10
        // SePtr<SeGraphNode> NextSibling @ 0x18
        // SePtr<SeGraphNode> EditParent(?) @ 0x20

        // ...but in serialized form, it only has a reference to the
        // UID of the parent node.
        int address = context.ReadInt32(offset);
        if (address != 0) ParentUid = context.ReadInt32(address);

        return offset + 0x28;
    }
}