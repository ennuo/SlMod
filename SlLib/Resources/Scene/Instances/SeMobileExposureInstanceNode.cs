using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeMobileExposureInstanceNode : SeInstanceNode
{
    public float BloomThreshold; // 0x80
    public float BloomOffset; // 0x84
    public float BloomScale; // 0x88
    public float WhiteLum; // 0x8c
    public float ExposureKey; // 0x90
    public float Saturation; // 0xbc
    public float ExposureScale; // 0x94
    
    public float ShScaleDir; // 0xb4
    
    public float ShScaleAmbient; // 0xb8

    public float ShScaleCarDir; // 0xc0
    public float ShScaleCarAmbient; // 0xc4
    
    public float CurvePoint1; // 0x98
    public float CurvePoint2; // 0xa0
    public float CurvePoint3; // 0xa8

    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        BloomThreshold = context.ReadFloat(0x80);
        BloomOffset = context.ReadFloat(0x84);
        BloomScale = context.ReadFloat(0x88);
        WhiteLum = context.ReadFloat(0x8c);
        ExposureKey = context.ReadFloat(0x90);
        Saturation = context.ReadFloat(0xbc);
        ExposureScale = context.ReadFloat(0x94);
        ShScaleDir = context.ReadFloat(0xb4);
        ShScaleAmbient = context.ReadFloat(0xb8);
        ShScaleCarDir = context.ReadFloat(0xc0);
        ShScaleCarAmbient = context.ReadFloat(0xc4);
        CurvePoint1 = context.ReadFloat(0x98);
        CurvePoint2 = context.ReadFloat(0xa0);
        CurvePoint3 = context.ReadFloat(0xa8);
    }
}