using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeDefinitionNode : SeGraphNode
{
    /// <summary>
    ///     Loads this definition node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        // int InstanceCount
        // 

        return offset + 0x18;
    }
}