using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff;

public class LensFlare2 : IResourceSerializable
{
    public Vector4 SunPosition;
    public bool WidescreenFlares;
    public float FullSunRatio;
    
    // These fields are included for completeness, but don't seem to be loaded by the game,
    // just being overridden with defaults.
    public float SunSize;
    public Vector3 SunColor;
    public float SunDispersion;
    
    public void Load(ResourceLoadContext context)
    {
        SunPosition = context.ReadFloat4();
        WidescreenFlares = context.ReadBoolean(wide: true);
        FullSunRatio = context.ReadFloat();

        SunSize = context.ReadFloat();
        SunColor = context.ReadFloat3();
        SunDispersion = context.ReadFloat();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat4(buffer, SunPosition, 0x0);
        context.WriteBoolean(buffer, WidescreenFlares, 0x10, wide: true);
        context.WriteFloat(buffer, FullSunRatio, 0x14);
        context.WriteFloat(buffer, SunSize, 0x18);
        context.WriteFloat3(buffer, SunColor, 0x1c);
        context.WriteFloat(buffer, SunDispersion, 0x28);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x2c;
    }
}