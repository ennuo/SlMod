namespace SlLib.Enums;

[Flags]
public enum VehicleFlags
{
    TriggerCar = 1 << 0,
    TriggerBoat = 1 << 1,
    TriggerPlane = 1 << 2,
    TriggerWeapon = 1 << 3,
    TriggerOnLoad = 1 << 5,
    TriggerPrediction = 1 << 6,
    TriggerHasJumboMap = 1 << 8,
    TriggerOncePerRacer = 1 << 9
};