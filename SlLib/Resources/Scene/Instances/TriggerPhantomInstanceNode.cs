using SlLib.Enums;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class TriggerPhantomInstanceNode : SeInstanceTransformNode
{
    public readonly TriggerPhantomHashInfo[] MessageText = new TriggerPhantomHashInfo[8];
    public readonly SeNodeBase?[] LinkedNode = new SeNodeBase[8];
    public bool Lap1, Lap2, Lap3, Lap4;
    public int Leader;
    public int NumActivations;
    public int PhantomFlags;
    public float PredictionTime;

    public void SetAllLapMasks(bool enabled)
    {
        Lap1 = enabled;
        Lap2 = enabled;
        Lap3 = enabled;
        Lap4 = enabled;
    }
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // old versions of TriggerPhantomInstanceNode extended SeInstanceEntityNode
        // so have to increase the offset by 0x20 to account for that
        int pos = context.Version <= 0xb ? 0x20 : 0x0;
        
        for (int i = 0; i < 8; ++i)
        {
            MessageText[i] = (TriggerPhantomHashInfo)context.ReadInt32(pos + 0x160 + (i * 0x4));
            LinkedNode[i] = context.LoadNode(context.ReadInt32(pos + 0x334 + (i * 4)));
        }

        Lap1 = context.ReadBoolean(pos + 0x180, wide: true);
        Lap2 = context.ReadBoolean(pos + 0x184, wide: true);
        Lap3 = context.ReadBoolean(pos + 0x188, wide: true);
        Lap4 = context.ReadBoolean(pos + 0x18c, wide: true);
        
        
        Leader = context.ReadInt32(pos + 0x354);
        NumActivations = context.ReadInt32(pos + 0x358);
        PhantomFlags = context.ReadInt32(pos + 0x35c);
        PredictionTime = context.ReadFloat(pos + 0x36c);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        for (int i = 0; i < 8; ++i)
        {
            context.WriteInt32(buffer, (int)MessageText[i], 0x160 + (i * 4));
            context.WriteInt32(buffer, LinkedNode[i]?.Uid ?? 0, 0x334 + (i * 4));
        }
        
        context.WriteBoolean(buffer, Lap1, 0x180, wide: true);
        context.WriteBoolean(buffer, Lap2, 0x184, wide: true);
        context.WriteBoolean(buffer, Lap3, 0x188, wide: true);
        context.WriteBoolean(buffer, Lap4, 0x18c, wide: true);
        
        context.WriteInt32(buffer, Leader, 0x354);
        context.WriteInt32(buffer, NumActivations, 0x358);
        context.WriteInt32(buffer, PhantomFlags, 0x35c);
        context.WriteFloat(buffer, PredictionTime, 0x36c);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x380;
}