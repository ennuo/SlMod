using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeDefinitionNode : SeGraphNode
{
    /// <summary>
    ///     The instances of this definition.
    /// </summary>
    public readonly List<SeInstanceNode> Instances = [];
    
    public static T CreateObject<T>() where T : SeDefinitionNode, new()
    {
        T node = new();
        node.SetNameWithTimestamp(typeof(T).Name);
        return node;
    }
    
    /// <summary>
    ///     Loads this definition node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        // instance count @ 0x68

        return offset + 0x18;
    }
}