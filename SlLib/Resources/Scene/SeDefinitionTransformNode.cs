using System.Numerics;
using SlLib.Resources.Database;
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
    public int InheritTransforms = 1;

    /// <summary>
    ///     Transform node flags.
    /// </summary>
    public int TransformFlags = 0x7fe;

    /// <summary>
    ///     Loads this transform node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to load from</param>
    /// <returns>The offset of the next class base</returns>
    protected override int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        Matrix4x4 matrix = context.ReadMatrix(0x80);
        Matrix4x4.Decompose(matrix, out Scale, out Rotation, out Translation);
        
        InheritTransforms = context.ReadBitset32(0xc0);
        TransformFlags = context.ReadBitset32(0xc4);
        
        return offset + 0x50;
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        Matrix4x4 local =
            Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Translation);
        
        // I forgot if this edit matrix is the local or global matrix,
        // although realistically it doesn't matter, since it's only used by their internal tooling.
        context.WriteMatrix(buffer, local, 0x80); 
        context.WriteInt32(buffer, InheritTransforms, 0xc0);
        context.WriteInt32(buffer, TransformFlags, 0xc4);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xd0;
}