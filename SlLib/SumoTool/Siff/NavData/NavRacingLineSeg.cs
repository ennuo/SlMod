using System.Buffers.Binary;
using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavRacingLineSeg : IResourceSerializable
{
    public Vector3 RacingLine;
    public Vector3 SafeRacingLine;
    public float RacingLineScalar = 0.5f;
    public float SafeRacingLineScalar = 0.5f;
    public NavWaypointLink? Link;
    public float RacingLineLength = 1.0f;
    public int TurnType;
    public float SmoothSideLeft;
    public float SmoothSideRight;
    
    public void Load(ResourceLoadContext context)
    {
        RacingLine = context.ReadAlignedFloat3();
        SafeRacingLine = context.ReadAlignedFloat3();
        RacingLineScalar = context.ReadFloat();
        SafeRacingLineScalar = context.ReadFloat();
        
        Link = context.LoadPointer<NavWaypointLink>();
        
        RacingLineLength = context.ReadFloat();
        TurnType = context.ReadInt32();

        SmoothSideLeft = context.ReadFloat();
        SmoothSideRight = context.ReadFloat();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, RacingLine, 0x0);
        context.WriteFloat3(buffer, SafeRacingLine, 0x10);
        context.WriteFloat(buffer, RacingLineScalar, 0x20);
        context.WriteFloat(buffer, SafeRacingLineScalar, 0x24);
        context.SavePointer(buffer, Link, 0x28, deferred: true);
        context.WriteFloat(buffer, RacingLineLength, 0x2c);
        
        // turn type is 0x34 in later versions?
        // 0x3c = float (1.0 by default)
        context.WriteInt32(buffer, TurnType, 0x30);
        context.WriteFloat(buffer, SmoothSideLeft, 0x34);
        context.WriteFloat(buffer, SmoothSideRight, 0x38);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x40;
    }
}