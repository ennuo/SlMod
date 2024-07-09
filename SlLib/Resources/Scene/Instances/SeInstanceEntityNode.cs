using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceEntityNode : SeInstanceTransformNode
{
    public int RenderLayer;
    public int Flags;
    public int InstanceFlags;
    
    /// <summary>
    ///     Loads this instance node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected override int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        Flags = context.ReadBitset32(0x170);
        RenderLayer = context.ReadInt32(0x174);
        InstanceFlags = context.ReadInt32(0x178);
        
        // i think the game basically controls all of these
        // so we don't have to worry about setting it?
        // 0x178 is some sort of mask?
        // only meshes that dont have these bits set are baked ones it seems,
        // wonder if it really matters
        // specifically 0x179
            // 0x20 = non shared model
            // 0x40 = non shared materials
        
        return offset + 0x20;
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, Flags, 0x170);
        context.WriteInt32(buffer, RenderLayer, 0x174);
        context.WriteInt32(buffer, InstanceFlags, 0x178);
        
        context.WriteInt32(buffer, 0xBADF00D, 0x17c);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x180;
}