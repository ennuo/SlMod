using SlLib.Resources.Database;
using SlLib.Utilities;

namespace SlLib.Resources.Scene.Instances;

using System.Numerics;
using SlLib.Serialization;

public abstract class SeInstanceTransformNode : SeInstanceNode
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
    ///     DEBUG: World Matrix
    /// </summary>
    public Matrix4x4 WorldMatrix;
    
    public int InheritTransforms = 1; // can sometimes be 3???
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

        // Just going to ignore all the other matrices, and only use the local matrix
        context.ReadMatrix(offset); // Initial state local matrix
        Matrix4x4 local = context.ReadMatrix(0xc0); // Local Matrix
        Matrix4x4.Decompose(local, out Scale, out Rotation, out Translation);
        WorldMatrix = context.ReadMatrix(0x100); // World Matrix
        
        InheritTransforms = context.ReadBitset32(0x150);
        TransformFlags = context.ReadBitset32(0x154);
        
        // 0x140
        
        // + 0x40 = Matrix4 = Edit Local
        // + 0xc0 = Matrix4 = Local
        // + 0x100 = Matrix4 = World
        
        // + 0x150 = int = InheritTransforms
        // + 0x154 = int = HasAnimChannel (Flags)
        
        return offset + 0xe0;
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
        context.WriteMatrix(buffer, local, 0xc0);
        context.WriteMatrix(buffer, WorldMatrix, 0x100);
        context.WriteInt32(buffer, InheritTransforms, 0x150);
        context.WriteInt32(buffer, TransformFlags, 0x154);
        
        context.WriteInt32(buffer, 0xBADF00D, 0x158);
        context.WriteInt32(buffer, 0xBADF00D, 0x15c);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x160;
}