using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceEntityNode : SeInstanceTransformNode, IResourceSerializable
{
    public int[] WindowMask = new int[5];
        
    public int RenderLayer;
    public int Flags;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
    
    /// <summary>
    ///     Loads this instance node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        for (int i = 0; i < WindowMask.Length; ++i)
            WindowMask[i] = context.ReadInt32(offset + (i * 0x4));
        
        Flags = context.ReadInt32(0x170);
        RenderLayer = context.ReadInt32(0x174);
        
        return offset + 0x20;
    }
}