using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeDefinitionTransformNode : SeDefinitionNode
{
    /// <summary>
    ///     The local translation of this node.
    /// </summary>
    public Vector3 Translation = Vector3.Zero;

    /// <summary>
    ///     The local rotation of this node.
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    ///     The local scale of this node.
    /// </summary>
    public Vector3 Scale = Vector3.One;

    /// <summary>
    ///     Whether this node should inherit parent transforms.
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
        
        Matrix4x4 matrix = context.ReadMatrix(offset);
        Matrix4x4.Decompose(matrix, out Scale, out Rotation, out Translation);
        
        InheritTransforms = context.ReadBoolean(offset + 0x40, true);
        TransformFlags = context.ReadInt32(offset + 0x44);
        
        return offset + 0x50;
    }
}