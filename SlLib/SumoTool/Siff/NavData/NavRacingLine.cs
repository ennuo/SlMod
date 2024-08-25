using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavRacingLine : IResourceSerializable
{
    public List<NavRacingLineSeg> Segments = [];
    public bool Looping = false;
    
    // minimap gen grabs racing lines segments if the waypoints have any of the drive mode flags?
    // used to calculate min/max ig
    
    // first 3 flags are always set?
    // (1 << 3)[0x08] = Plane
    // (1 << 4)[0x10] = Car
    // (1 << 5)[0x20] = Boat
    // (1 << 6)[0x40] = ???
    // (1 << 7)[0x080] = disabled
    // (1 << 8)[0x100] = ???
    // (1 << 9)[0x200] = disabled permanently
    
    public int Permissions = 0x17;
    public float TotalLength;
    public float TotalBaseTime;
    
    public void Load(ResourceLoadContext context)
    {
        Segments = context.LoadArrayPointer<NavRacingLineSeg>(context.ReadInt32());
        Looping = context.ReadBoolean(wide: true);
        Permissions = context.ReadInt32();
        TotalLength = context.ReadFloat();
        TotalBaseTime = context.ReadFloat();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Segments.Count, 0x0);
        context.SaveReferenceArray(buffer, Segments, 0x4, align: 0x10);
        context.WriteBoolean(buffer, Looping, 0x8, wide: true);
        context.WriteInt32(buffer, Permissions, 0xc);
        context.WriteFloat(buffer, TotalLength, 0x10);
        context.WriteFloat(buffer, TotalBaseTime, 0x14);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}