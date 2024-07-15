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
        
        Links = context.LoadArray(waypointLinkData, numWaypointLinks, () => context.LoadPointer<NavWaypointLink>()!);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}