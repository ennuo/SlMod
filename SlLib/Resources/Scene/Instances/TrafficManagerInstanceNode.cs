using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class TrafficManagerInstanceNode : SeInstanceNode
{
    public SeNodeBase? Spline;
    public float Speed;
    public float RoadWidth;
    public int MaxTraffic;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        Spline = context.LoadNode(context.ReadInt32(0x88));
        Speed = context.ReadFloat(0x8c);
        RoadWidth = context.ReadFloat(0x90);
        MaxTraffic = context.ReadInt32(0x94);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, Spline?.Uid ?? 0, 0x88);
        context.WriteFloat(buffer, Speed, 0x8c);
        context.WriteFloat(buffer, RoadWidth, 0x90);
        context.WriteInt32(buffer, MaxTraffic, 0x94);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xac;
}