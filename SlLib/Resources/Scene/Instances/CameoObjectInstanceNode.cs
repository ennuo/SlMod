using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class CameoObjectInstanceNode : SeInstanceTransformNode
{
    public int Type;
    public float ActivateRadius;
    public float DeActivateRadius;
    public SeNodeBase? Spline;
    public float SpawnTime;
    public float SplineSpeed;
    public float SplineStartPos;
    public int CameoFlags;
    public float MinSplineSpeed;
    public SeNodeBase? DropMeshEntity;
    public int InitialCameoFlags;
    public float DebugSplinePos;
    
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        if (context.Version <= 0xb)
        {
            Type = context.ReadInt32(0x180 + 0x20);
            ActivateRadius = context.ReadFloat(0x180 + 0x24);
            return;
        }
        
        Type = context.ReadInt32(0x180);
        ActivateRadius = context.ReadFloat(0x18c);
        DeActivateRadius = context.ReadFloat(0x190);

        Spline = context.LoadNode(context.ReadInt32(0x1a4));

        SpawnTime = context.ReadFloat(0x1c4);
        SplineSpeed = context.ReadFloat(0x1c8);
        SplineStartPos = context.ReadFloat(0x1d4);

        CameoFlags = context.ReadInt32(0x1d8);
        MinSplineSpeed = context.ReadFloat(0x1dc);
        
        DropMeshEntity = context.LoadNode(context.ReadInt32(0x1e8));

        InitialCameoFlags = context.ReadInt32(0x210);
        DebugSplinePos = context.ReadFloat(0x214);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, Type, 0x180);
        context.WriteFloat(buffer, ActivateRadius, 0x18c);
        context.WriteFloat(buffer, DeActivateRadius, 0x190);
        context.WriteInt32(buffer, Spline?.Uid ?? 0, 0x1a4);
        context.WriteFloat(buffer, SpawnTime, 0x1c4);
        context.WriteFloat(buffer, SplineSpeed, 0x1c8);
        context.WriteFloat(buffer, SplineStartPos, 0x1d4);
        context.WriteInt32(buffer, CameoFlags, 0x1d8);
        context.WriteFloat(buffer, MinSplineSpeed, 0x1dc);
        context.WriteInt32(buffer, DropMeshEntity?.Uid ?? 0, 0x1e8);
        context.WriteInt32(buffer, InitialCameoFlags, 0x210);
        context.WriteFloat(buffer, DebugSplinePos, 0x214);
        
        // Default constructor parameters, no idea what they are, but just
        // going to make sure they're set anyway, don't think they matter for editing purposes,
        // but since structures are basically just referenced directly from the file instead of
        // serialized, need to make sure the state stays in-tact
        context.WriteInt16(buffer, 1, 0x1b4);
        context.WriteInt16(buffer, 1, 0x1f8);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x220;
}