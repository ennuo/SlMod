using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavStart : IResourceSerializable
{
    public int DriveMode;
    // 10 bytes for each racer in grid?
    // like 7 floats
    public NavTrackMarker? TrackMarker;
    // float
    
    public void Load(ResourceLoadContext context)
    {
        DriveMode = context.ReadInt32();
        context.Position += 0x2c;
        TrackMarker = context.LoadPointer<NavTrackMarker>();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, DriveMode, 0x0);
        context.SavePointer(buffer, TrackMarker, 0x30, align: 0x10, deferred: true);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x40;
    }
}