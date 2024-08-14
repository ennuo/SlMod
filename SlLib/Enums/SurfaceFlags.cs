namespace SlLib.Enums;

[Flags]
public enum SurfaceFlags
{
    Sticky = 0x1,
    WeakSticky = 0x20,
    DoubleGravity = 0x40,
    SpaceWarp = 0x80,
    
    CatchupDisabled = 0x10000000,
    StuntsDisabled = 0x20000000,
    BoostDisabled = 0x40000000
}