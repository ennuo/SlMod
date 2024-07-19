using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuEmitterData : IResourceSerializable
{
    public int BranchIndex;
    public int Type;
    public float SpeedMin, SpeedMax;
    public float LifeMin, LifeMax;
    public int MaxCount;
    public float Spread;
    public Vector4 Direction;
    public float RandomDirection;
    public float DirectionalSpeed;
    public int FaceVelocity;
    public float InheritFactor;
    public float ConserveFactor;
    public float ConvertorFactor;
    public int ParticleBranch;
    public float Curviness;
    public int UseFog;
    public int Refraction;
    public int VolumeShape;
    public float VolumeSweep;
    public float AwayFromCenter;
    public float AlongAxis;
    public float AroundAxis;
    public float AwayFromAxis;
    public float TangentSpeed;
    public float NormalSpeed;
    public float Rotation;
    public float RotateSpeed;
    public int Color;
    public int RandomColor;
    public float Width;
    public float Height;
    public float RandomScale;
    public float ScrollSpeedU;
    public float ScrollSpeedV;
    public int Layer;
    public int PlaneMode;
    public float PlaneConst;
    public Vector4 PlaneNormal;
    public int Flags;
    public List<SuField> Fields = [];
    public SuRamp? ColorRamp;
    public SuRamp? WidthRamp;
    public SuRamp? HeightRamp;
    public SuCurve? Curve;
    public SuRenderMaterial? Material;
    public SuRenderTexture? Texture;
    
    public int[] AnimatedData = new int[2];
    
    public void Load(ResourceLoadContext context)
    {
        BranchIndex = context.ReadInt32();
        Type = context.ReadInt32();
        SpeedMin = context.ReadFloat();
        SpeedMax = context.ReadFloat();
        LifeMin = context.ReadFloat();
        LifeMax = context.ReadFloat();
        MaxCount = context.ReadInt32();
        Spread = context.ReadFloat();
        Direction = context.ReadFloat4();
        RandomDirection = context.ReadFloat();
        DirectionalSpeed = context.ReadFloat();
        FaceVelocity = context.ReadInt32();
        InheritFactor = context.ReadFloat();
        ConserveFactor = context.ReadFloat();
        ConvertorFactor = context.ReadFloat();
        context.Position += 8; // reserved0,1
        ParticleBranch = context.ReadInt32();
        Curviness = context.ReadFloat();
        UseFog = context.ReadInt32();
        Refraction = context.ReadInt32();
        VolumeShape = context.ReadInt32();
        VolumeSweep = context.ReadFloat();
        AwayFromCenter = context.ReadFloat();
        AlongAxis = context.ReadFloat();
        AroundAxis = context.ReadFloat();
        AwayFromAxis = context.ReadFloat();
        TangentSpeed = context.ReadFloat();
        NormalSpeed = context.ReadFloat();
        Rotation = context.ReadFloat();
        RotateSpeed = context.ReadFloat();
        Color = context.ReadInt32();
        RandomColor = context.ReadInt32();
        Width = context.ReadFloat();
        Height = context.ReadFloat();
        RandomScale = context.ReadFloat();
        ScrollSpeedU = context.ReadFloat();
        ScrollSpeedV = context.ReadFloat();
        Layer = context.ReadInt32();
        PlaneMode = context.ReadInt32();
        PlaneConst = context.ReadFloat();
        PlaneNormal = context.ReadFloat4();
        Flags = context.ReadInt32();

        Fields = context.LoadArrayPointer<SuField>(context.ReadInt32());
        ColorRamp = context.LoadPointer<SuRamp>();
        WidthRamp = context.LoadPointer<SuRamp>();
        HeightRamp = context.LoadPointer<SuRamp>();
        Curve = context.LoadPointer<SuCurve>();
        Material = context.LoadPointer<SuRenderMaterial>();
        Texture = context.LoadPointer<SuRenderTexture>();

        AnimatedData[0] = context.ReadInt32();
        AnimatedData[1] = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, BranchIndex, 0x0);
        context.WriteInt32(buffer, Type, 0x4);
        context.WriteFloat(buffer, SpeedMin, 0x8);
        context.WriteFloat(buffer, SpeedMax, 0xc);
        context.WriteFloat(buffer, LifeMin, 0x10);
        context.WriteFloat(buffer, LifeMax, 0x14);
        context.WriteInt32(buffer, MaxCount, 0x18);
        context.WriteFloat(buffer, Spread, 0x1c);
        context.WriteFloat4(buffer, Direction, 0x20);
        context.WriteFloat(buffer, RandomDirection, 0x30);
        context.WriteFloat(buffer, DirectionalSpeed, 0x34);
        context.WriteInt32(buffer, FaceVelocity, 0x38);
        context.WriteFloat(buffer, InheritFactor, 0x3c);
        context.WriteFloat(buffer, ConserveFactor, 0x40);
        context.WriteFloat(buffer, ConvertorFactor, 0x44);
        context.WriteInt32(buffer, ParticleBranch, 0x50);
        context.WriteFloat(buffer, Curviness, 0x54);
        context.WriteInt32(buffer, UseFog, 0x58);
        context.WriteInt32(buffer, Refraction, 0x5c);
        context.WriteInt32(buffer, VolumeShape, 0x60);
        context.WriteFloat(buffer, VolumeSweep, 0x64);
        context.WriteFloat(buffer, AwayFromCenter, 0x68);
        context.WriteFloat(buffer, AlongAxis, 0x6c);
        context.WriteFloat(buffer, AroundAxis, 0x70);
        context.WriteFloat(buffer, AwayFromAxis, 0x74);
        context.WriteFloat(buffer, TangentSpeed, 0x78);
        context.WriteFloat(buffer, NormalSpeed, 0x7c);
        context.WriteFloat(buffer, Rotation, 0x80);
        context.WriteFloat(buffer, RotateSpeed, 0x84);
        context.WriteInt32(buffer, Color, 0x88);
        context.WriteInt32(buffer, RandomColor, 0x8c);
        context.WriteFloat(buffer, Width, 0x90);
        context.WriteFloat(buffer, Height, 0x94);
        context.WriteFloat(buffer, RandomScale, 0x98);
        context.WriteFloat(buffer, ScrollSpeedU, 0x9c);
        context.WriteFloat(buffer, ScrollSpeedV, 0xa0);
        context.WriteInt32(buffer, Layer, 0xa4);
        context.WriteInt32(buffer, PlaneMode, 0xa8);
        context.WriteFloat(buffer, PlaneConst, 0xac);
        context.WriteFloat4(buffer, PlaneNormal, 0xb0);
        context.WriteInt32(buffer, Flags, 0xc0);

        
        context.WriteInt32(buffer, Fields.Count, 0xc4);
        context.SaveReferenceArray(buffer, Fields, 0xc8, align: 0x10);
        
        // TODO: where the fuck is this "deferred" to
        context.SavePointer(buffer, ColorRamp, 0xcc, deferred: true);
        context.SavePointer(buffer, WidthRamp, 0xd0, deferred: true);
        context.SavePointer(buffer, HeightRamp, 0xd4, deferred: true);
        
        context.SavePointer(buffer, Curve, 0xd8, deferred: true, align: 0x10);
        context.SavePointer(buffer, Material, 0xdc);
        context.SavePointer(buffer, Texture, 0xe0);
        
        context.WriteInt32(buffer, AnimatedData[0], 0xe4);
        context.WriteInt32(buffer, AnimatedData[1], 0xe8);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xF0;
    }
}