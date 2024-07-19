using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavRacingLineRef : IResourceSerializable
{
    public NavRacingLine? RacingLine;
    public int SegmentIndex;
    
    public void Load(ResourceLoadContext context)
    {
        RacingLine = context.LoadPointer<NavRacingLine>();
        SegmentIndex = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SavePointer(buffer, RacingLine, 0x0, align: 0x10, deferred: true);
        context.WriteInt32(buffer, SegmentIndex, 0x4);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}