using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeDefinitionTransformNode : SeDefinitionNode
{
    /// <summary>
    ///     The local transform of this node.
    /// </summary>
    public Matrix4x4 Transform;

    /// <summary>
    ///     Whether or not this node should inherit parent transforms.
    /// </summary>
    public bool InheritTransforms;

    /// <summary>
    ///     Transform node flags.
    /// </summary>
    public int TransformFlags;

    /// <summary>
    ///     Loads this transform node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to load from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        Transform = context.ReadMatrix(offset);
        InheritTransforms = context.ReadBoolean(offset + 0x40, true);
        TransformFlags = context.ReadInt32(offset + 0x44);

        return offset + 0x50;
    }
}