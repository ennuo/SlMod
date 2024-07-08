using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class TriggerPhantomInstanceNode : SeInstanceTransformNode, IResourceSerializable
{
    public readonly MessageType[] MessageText = new MessageType[8];
    public readonly SeNodeBase?[] LinkedNode = new SeNodeBase[8];
    public bool Lap1, Lap2, Lap3, Lap4;
    public int Leader;
    public int NumActivations;
    public int Flags;
    public float PredictionTime;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        for (int i = 0; i < 8; ++i)
        {
            MessageText[i] = (MessageType)context.ReadInt32(0x160 + (i * 0x4));
            LinkedNode[i] = context.LoadNode(context.ReadInt32(0x334 + (i * 4)));
        }

        Lap1 = context.ReadBoolean(0x180, wide: true);
        Lap2 = context.ReadBoolean(0x184, wide: true);
        Lap3 = context.ReadBoolean(0x188, wide: true);
        Lap4 = context.ReadBoolean(0x18c, wide: true);
        
        
        Leader = context.ReadInt32(0x354);
        NumActivations = context.ReadInt32(0x358);
        Flags = context.ReadInt32(0x35c);
        PredictionTime = context.ReadFloat(0x36c);
    }
    
    public enum LeaderType
    {
	    None = 0,
	    Leader = 1,
	    LeadingHuman = 2,
	    LeadingAi = 3,
	    LeadingLocal = 4
    }
    
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
    
	public enum MessageType
	{
		Empty = 0,
		AirBoost = 189157517,
		BoostPad = -2042409423,
		BoatBoostGate = 125346184,
		TransformCar = -841982324,
		TransformPlane = 118760201,
		TransformBoat = 210079875,
		LandPlane = -1876715491,
		SpinOut = -1213970453,
		SpinOutHeavy = 870623274,
		SecondaryWeaponEffect = -1540354832,
		Respot = -1558689353,
		MineExplosionFX = 1952503777,
		Explosion = 1070353362,
		Squash = 1498295247,
		SquashHeavy = -1420524006,
		SlowdownShunt = -696405373,
		SlowdownShuntLight = -1383978361,
		FillWater = -226110868,
		HideNode = 709647355,
		ShowNode = 469498329,
		EnableNode = -1384115316,
		DisableNode = -37455467,
		ForceTransformCar = -317693437,
		PlayAudio = 1759330814,
		FadeAudio = -1544848447,
		PlayAnimationLink = 1540381204,
		CameraShake = 2101732718,
		TriggerParticleLink = -733180385,
		ScreenSplash = -398790126,
		CollectAnObject = 1568854034,
		DropAnObject = -320711608,
		Teleport = 1084257758,
		TimedTeleport = 994238613,
		TargetSmash = -1048956908,
		SwitchToCarRoute = 1302463695,
		SwitchToBoatRoute = 691702127,
		SwitchToPlaneRoute = -459876044,
		SwitchToExclusiveCarRoute = 1200708874,
		SwitchToExclusiveBoatRoute = -558361262,
		SwitchToExclusivePlaneRoute = 1829363952,
		AiObstacle = -1125608476,
		AiGenericTarget = -1806459307,
		EnableRacingLines = 1771726842,
		DisableRacingLines = 317789545,
		EnableRacingLinesPermanently = -1739858317,
		DisableRacingLinesPermanently = 931038684,
		SwitchToOpenRoute = -760816338,
		TransformCarPrompt = 891162648,
		TransformBoatPrompt = 823031447,
		TransformPlanePrompt = 1700585297,
		AudioSwoosh_SmallObject_Pan = -1760910791,
		AudioSwoosh_MediumObject_Pan = -1445879522,
		AudioSwoosh_LargeObject_Pan = 1194761607,
		AudioSwoosh_SmallObject_Center = 15450130,
		AudioSwoosh_MediumObject_Center = 307414316,
		AudioSwoosh_LargeObject_Center = -1418509929,
		AudioSwoosh_EnterTunnel = 524898988,
		AudioSwoosh_ExitTunnel = -1331351440,
		Cameo_EnableSplineMovement = -40143073,
		EnableTag = 581302922,
		DisableTag = -377967812,
		TransformRespot = -2048107817,
		NightsRing = 1994748133,
		NightsRingFinal = 312533218,
		ForceLap1 = -1875627780,
		ForceLap2 = 2079250086,
		ForceLap3 = 1674309033,
		TriggerNode = 329379503,
		Electrify_LightSlowdown = 1227761862,
		Electrify_HeavySlowdown = 75749318,
		TriggerLightSet1 = 324923854,
		TriggerLightSet2 = -1526649043,
		TriggerLightSet3 = -1996236308,
		EnableForRacerViewport = 1667948044,
		DisableForRacerViewport = 55470186,
		ForceTriggerPhantom = -260391570,
	}
}