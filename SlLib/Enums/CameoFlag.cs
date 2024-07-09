namespace SlLib.Enums;

[Flags]
public enum CameoFlag
{
    RespawnAtEndOfSpline = 1 << 0,
    AddRemainingTimeToSpawn = 1 << 1,
    NeverRespawn = 1 << 2,
    SplineTrackLeaderSpeed = 1 << 3,
    DontMoveOnSplineIdle = 1 << 4,
    DisableSplineMovement = 1 << 5,
    UseSplineRollValue = 1 << 6,
    AttackActive = 1 << 7
}