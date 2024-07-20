using System.Numerics;
using SlLib.Enums;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class WeaponPodInstanceNode : SeInstanceTransformNode
{
    public Vector3 PodColor;
    public WeaponPodMessage Message;
    public int AllocationCount;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // old versions extended SeInstanceEntityNode
        // so have to increase the offset by 0x20 to account for that
        int pos = context.Version <= 0x1b ? 0x20 : 0x0;
        
        PodColor = context.ReadFloat3(pos + 0x160) / 255.0f;
        Message = (WeaponPodMessage)context.ReadInt32(pos + 0x170);
        AllocationCount = context.ReadInt32(pos + 0x174);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteFloat3(buffer, PodColor * 255.0f, 0x160);
        context.WriteInt32(buffer, (int)Message, 0x170);
        context.WriteInt32(buffer, AllocationCount, 0x174);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x380;
}