using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuLightData : IResourceSerializable
{
    public int Type;
    public float InnerConeAngle;
    public float OuterConeAngle;
    public int Falloff;
    public Vector4 ShadowColor;
    public bool IsShadowLight;
    public int BranchIndex;
    public SuRenderTexture? Texture;
    // SuAnimatedFloatData
    
    public void Load(ResourceLoadContext context)
    {
        Type = context.ReadInt32();
        InnerConeAngle = context.ReadFloat();
        OuterConeAngle = context.ReadFloat();
        Falloff = context.ReadInt32();
        ShadowColor = context.ReadFloat4();
        IsShadowLight = context.ReadBoolean(wide: true);
        BranchIndex = context.ReadInt32();
        Texture = context.LoadPointer<SuRenderTexture>();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Type, 0x0);
        context.WriteFloat(buffer, InnerConeAngle, 0x4);
        context.WriteFloat(buffer, OuterConeAngle, 0x8);
        context.WriteInt32(buffer, Falloff, 0xc);
        context.WriteFloat4(buffer, ShadowColor, 0x10);
        context.WriteBoolean(buffer, IsShadowLight, 0x20);
        context.WriteInt32(buffer, BranchIndex, 0x24);
        context.SavePointer(buffer, Texture, 0x28);
        
        // SuAnimatedFloatData deal with that at some ponit
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x40;
    }
}