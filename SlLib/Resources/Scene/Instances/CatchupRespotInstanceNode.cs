using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class CatchupRespotInstanceNode : SeInstanceTransformNode
{
    public int Lap;
    public int DriveMode;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        Lap = context.ReadInt32(0x160);
        DriveMode = context.ReadInt32(0x164);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, Lap, 0x160);
        context.WriteInt32(buffer, DriveMode, 0x164);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x170;
}