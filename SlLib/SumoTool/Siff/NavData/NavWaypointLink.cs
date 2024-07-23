using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavWaypointLink : IResourceSerializable
{
    public Vector3 FromToNormal;
    public Vector3 Right;
    public Vector3 Left;
    public Vector3 RacingLineLimitLeft;
    public Vector3 RacingLineLimitRight;
    public Plane3 Plane;
    public float RacingLineLeftScalar;
    public float RacingLineRightScalar;
    
    public NavWaypoint? From;
    public NavWaypoint? To;

    public float Length;
    public float Width;
    public List<Vector3> CrossSection = [];
    public List<NavRacingLineRef> RacingLines = [];
    public NavSpatialGroup? SpatialGroup;
    
    
    public void Load(ResourceLoadContext context)
    {
        FromToNormal = context.ReadAlignedFloat3();
        Right = context.ReadAlignedFloat3();
        Left = context.ReadAlignedFloat3();
        RacingLineLimitLeft = context.ReadAlignedFloat3();
        RacingLineLimitRight = context.ReadAlignedFloat3();
        Plane = context.LoadObject<Plane3>();
        RacingLineLeftScalar = context.ReadFloat();
        RacingLineRightScalar = context.ReadFloat();
        From = context.LoadPointer<NavWaypoint>();
        To = context.LoadPointer<NavWaypoint>();

        Length = context.ReadFloat();
        Width = context.ReadFloat();
        
        CrossSection = context.LoadArrayPointer(context.ReadInt32(), context.ReadAlignedFloat3);
        
        Console.WriteLine(CrossSection.Count);
        
        if (context.ReadPointer() != 0)
            throw new SerializationException("wawawaw");

        int racingLineData = context.ReadPointer();
        RacingLines = context.LoadArray<NavRacingLineRef>(racingLineData, context.ReadInt32());
        SpatialGroup = context.LoadPointer<NavSpatialGroup>();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, FromToNormal, 0x0);
        context.WriteFloat3(buffer, Right, 0x10);
        context.WriteFloat3(buffer, Left, 0x20);
        context.WriteFloat3(buffer, RacingLineLimitLeft, 0x30);
        context.WriteFloat3(buffer, RacingLineLimitRight, 0x40);
        context.SaveObject(buffer, Plane, 0x50);
        context.WriteFloat(buffer, RacingLineLeftScalar, 0x70);
        context.WriteFloat(buffer, RacingLineRightScalar, 0x74);
        context.SavePointer(buffer, From, 0x78, align: 0x10, deferred: true);
        context.SavePointer(buffer, To, 0x7c, align: 0x10, deferred: true);
        context.WriteFloat(buffer, Length, 0x80);
        context.WriteFloat(buffer, Width, 0x84);
        
        context.WriteInt32(buffer, CrossSection.Count, 0x8c);
        ISaveBuffer csData = context.SaveGenericPointer(buffer, 0x8c, CrossSection.Count * 0x10, align: 0x10);
        for (int i = 0; i < CrossSection.Count; ++i)
            context.WriteFloat3(csData, CrossSection[i], i * 0x10);
        
        context.WriteInt32(buffer, RacingLines.Count, 0x98);
        context.SaveReferenceArray(buffer, RacingLines, 0x94);
        context.SavePointer(buffer, SpatialGroup, 0x98, deferred: true);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xa0;
    }
}