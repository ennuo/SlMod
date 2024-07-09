using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceTimelineNode : SeInstanceNode
{
    public float EndTime;
    public float PauseAt;
    public bool DisableAtEnd = true;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        EndTime = context.ReadFloat(0x80);
        PauseAt = context.ReadFloat(0x84);
        DisableAtEnd = context.ReadBoolean(0x88, wide: true);
        // int ???
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteFloat(buffer, EndTime, 0x80);
        context.WriteFloat(buffer, PauseAt, 0x84);
        context.WriteBoolean(buffer, DisableAtEnd, 0x88, wide: true);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x90;
}