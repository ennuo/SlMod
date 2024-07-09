namespace SlLib.Enums;

[Flags]
public enum LightDataFlag
{
    BoxLight = 1 << 0,
    ShadowLight = 1 << 7,
    LightGroup0 = 1 << 15,
    LightGroup1 = 1 << 16,
    LightGroup2 = 1 << 17,
    LightGroup3 = 1 << 18,
    LightGroup4 = 1 << 19,
    LightGroup5 = 1 << 20,
    LightUser0 = 1 << 23,
    LightUser1 = 1 << 24,
    LightOverlays = 1 << 25
}