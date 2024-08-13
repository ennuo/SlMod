using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavSpatialGroup : IResourceSerializable
{
    public List<NavWaypointLink> Links = [];
    
    // There's also a field for NavRacingLineTracker and a LastSearchID
    // but these aren't important to be serialized.
    
    public void Load(ResourceLoadContext context)
    {
        int numWaypointLinks = context.ReadInt32();
        int waypointLinkData = context.ReadPointer();
        Links = context.LoadPointerArray<NavWaypointLink>(waypointLinkData, numWaypointLinks);
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Links.Count, 0x0);
        context.SavePointerArray(buffer, Links, 0x4, elementAlignment: 0x10, deferred: true);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}