using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class DynamicObjectInstanceNode : SeInstanceTransformNode
{
    public int Type;
    public float Radius1, Radius2;
    public float Time1, Time2;
    public float Value1, Value2, Value3, Value4;
    public SeNodeBase? Spline;
    public float Speed;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Type = context.ReadInt32(0x180);
        Radius1 = context.ReadFloat(0x19c);
        Radius2 = context.ReadFloat(0x1a0);
        Time1 = context.ReadFloat(0x1a4);
        Time2 = context.ReadFloat(0x1a8);
        Value1 = context.ReadFloat(0x1ac);
        Value2 = context.ReadFloat(0x1b0);
        Value3 = context.ReadFloat(0x1b4);
        Value4 = context.ReadFloat(0x1b8);
        Spline = context.LoadNode(context.ReadInt32(0x1c4));
        Speed = context.ReadFloat(0x1c8);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, Type, 0x180);
        context.WriteFloat(buffer, Radius1, 0x19c);
        context.WriteFloat(buffer, Radius2, 0x1a0);
        context.WriteFloat(buffer, Time1, 0x1a4);
        context.WriteFloat(buffer, Time2, 0x1a8);
        context.WriteFloat(buffer, Value1, 0x1ac);
        context.WriteFloat(buffer, Value2, 0x1b0);
        context.WriteFloat(buffer, Value3, 0x1b4);
        context.WriteFloat(buffer, Value4, 0x1b8);
        context.WriteInt32(buffer, Spline?.Uid ?? 0, 0x1c4);
        context.WriteFloat(buffer, Speed, 0x1c8);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1d0;
}