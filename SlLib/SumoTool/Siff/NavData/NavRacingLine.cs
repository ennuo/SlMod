using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavRacingLine : IResourceSerializable
{
    public List<NavRacingLineSeg> Segments = [];
    public bool Looping;
    public int Permissions;
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
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}