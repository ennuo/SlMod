using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleStyleNode : SeDefinitionNode
{
    public Vector4 Scale;
    public Vector4 ScaleRandom;
    public Vector4 Colour;
    public Vector4 ColourRandom;
    public float Lifetime;
    public float LifetimeRandom;
    public bool LockScaleAxes
     {
        get => (StyleFlags & (1 << 0)) != 0;
        set
        {
            if (value) StyleFlags &= ~(1 << 0);
            else StyleFlags |= (1 << 0);
        }
    }

    public bool LockColourChannels
     {
        get => (StyleFlags & (1 << 1)) != 0;
        set
        {
            if (value) StyleFlags &= ~(1 << 1);
            else StyleFlags |= (1 << 1);
        }
    }

    public bool AsHSV
     {
        get => (StyleFlags & (1 << 2)) != 0;
        set
        {
            if (value) StyleFlags &= ~(1 << 2);
            else StyleFlags |= (1 << 2);
        }
    }

    public int StyleFlags;
    public Vector4 UVTilesRect;
    public int UVMode;
    public float TileAnimStart;
    public float TileAnimDuration;
    public int AlphaMode;
    public float AlphaFadeStart;
    public float AlphaFadeDuration;
    public float AlphaFadeValA;
    public float AlphaFadeValB;
    public int ScaleMode;
    public float ScaleFadeStart;
    public float ScaleFadeDuration;
    public float ScaleFadeValA;
    public float ScaleFadeValB;
    public float Acceleration;
    public float ScalePerSecond;
    public bool TileAnimTimeAsLifetime
     {
        get => (NumTilesX & (1 << 0)) != 0;
        set
        {
            if (value) NumTilesX &= ~(1 << 0);
            else NumTilesX |= (1 << 0);
        }
    }

    public bool AlphaFadeAsLifetime
     {
        get => (NumTilesX & (1 << 1)) != 0;
        set
        {
            if (value) NumTilesX &= ~(1 << 1);
            else NumTilesX |= (1 << 1);
        }
    }

    public bool ScaleFadeAsLifetime
     {
        get => (NumTilesX & (1 << 2)) != 0;
        set
        {
            if (value) NumTilesX &= ~(1 << 2);
            else NumTilesX |= (1 << 2);
        }
    }

    public int TileAnimLoops;
    public int NumTilesX;
    public int NumTilesY;
    public bool AlphaNoise
     {
        get => (NumTilesX & (1 << 27)) != 0;
        set
        {
            if (value) NumTilesX &= ~(1 << 27);
            else NumTilesX |= (1 << 27);
        }
    }

    public bool ScaleNoise
     {
        get => (NumTilesX & (1 << 28)) != 0;
        set
        {
            if (value) NumTilesX &= ~(1 << 28);
            else NumTilesX |= (1 << 28);
        }
    }

    public int UpdateFlags;
    public float AlphaNoiseBase;
    public float AlphaNoiseRange;
    public float AlphaNoiseSpeed;
    public float ScaleNoiseBase;
    public float ScaleNoiseRange;
    public float ScaleNoiseSpeed;
    public float RotOffsetX;
    public float RotOffsetY;
    public int RenderBlendOp;
    public int FaceMode;
    public bool DepthWrite
     {
        get => (AlphaRef & (1 << 8)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 8);
            else AlphaRef |= (1 << 8);
        }
    }

    public int AlphaRef;
    public bool ScreenRGB
     {
        get => (AlphaRef & (1 << 17)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 17);
            else AlphaRef |= (1 << 17);
        }
    }

    public bool ScreenRGBAspect
     {
        get => (AlphaRef & (1 << 18)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 18);
            else AlphaRef |= (1 << 18);
        }
    }

    public bool SoftParticles
     {
        get => (AlphaRef & (1 << 19)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 19);
            else AlphaRef |= (1 << 19);
        }
    }

    public int BackfaceCulling;
    public bool TonemappingOff
     {
        get => (AlphaRef & (1 << 22)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 22);
            else AlphaRef |= (1 << 22);
        }
    }

    public bool LitShadow
     {
        get => (AlphaRef & (1 << 23)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 23);
            else AlphaRef |= (1 << 23);
        }
    }

    public bool PixelShadow
     {
        get => (AlphaRef & (1 << 24)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 24);
            else AlphaRef |= (1 << 24);
        }
    }

    public bool MaxClipSizeDisabed
     {
        get => (AlphaRef & (1 << 25)) != 0;
        set
        {
            if (value) AlphaRef &= ~(1 << 25);
            else AlphaRef |= (1 << 25);
        }
    }

    public int MaterialMode;
    public int UserDataFlags;
    public float VelStretchMax;
    public Vector4 ColourAdd;
    public Vector4 ColourMul;
    public float ScreenRGBScX;
    public float ScreenRGBScY;
    public float SoftParticlesOffset;
    public float SoftParticlesMultiply;
    public float ClipNearStart;
    public float ClipNearDist;
    public float ClipNFarStart;
    public float ClipFarDist;
    public float ParticleRefractionMul;
    public float LightAmbientMul;
    public float LightMul;
    public float MaxClipSize;
    public Vector4 CameraOffset;
    public int MaxParticlesPerContainer;
    public bool AffectorSet0
     {
        get => (AffectorSets & (1 << 0)) != 0;
        set
        {
            if (value) AffectorSets &= ~(1 << 0);
            else AffectorSets |= (1 << 0);
        }
    }

    public bool AffectorSet1
     {
        get => (AffectorSets & (1 << 1)) != 0;
        set
        {
            if (value) AffectorSets &= ~(1 << 1);
            else AffectorSets |= (1 << 1);
        }
    }

    public bool AffectorSet2
     {
        get => (AffectorSets & (1 << 2)) != 0;
        set
        {
            if (value) AffectorSets &= ~(1 << 2);
            else AffectorSets |= (1 << 2);
        }
    }

    public bool AffectorSet3
     {
        get => (AffectorSets & (1 << 3)) != 0;
        set
        {
            if (value) AffectorSets &= ~(1 << 3);
            else AffectorSets |= (1 << 3);
        }
    }
    
    // todo: fixup the ramp node type

    public int AffectorSets;
    public bool UVRectAsTiles;
    public int UVRectAsTilesDim;
    public SeDefinitionTextureNode? Texture;
    public string TextureName;
    public SeDefinitionParticleEmitterNode? SpawnEventEmitter;
    public string SpawnEventEmitterName;
    public SeNodeBase? ColourRamp; // SeDefinitionRampNodeVector4
    public string ColourRampName;
    public SeNodeBase? ScaleRamp; // SeDefinitionRampNodeVector4
    public string ScaleRampName;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Scale = context.ReadFloat4(0x80);
        ScaleRandom = context.ReadFloat4(0x90);
        Colour = context.ReadFloat4(0xa0);
        ColourRandom = context.ReadFloat4(0xb0);
        Lifetime = context.ReadFloat(0xc0);
        LifetimeRandom = context.ReadFloat(0xc4);
        StyleFlags = context.ReadInt32(0xc8);
        UVTilesRect = context.ReadFloat4(0xd0);
        UVMode = context.ReadInt32(0xe0);
        TileAnimStart = context.ReadFloat(0xe4);
        TileAnimDuration = context.ReadFloat(0xe8);
        AlphaMode = context.ReadInt32(0xec);
        AlphaFadeStart = context.ReadFloat(0xf0);
        AlphaFadeDuration = context.ReadFloat(0xf4);
        AlphaFadeValA = context.ReadFloat(0xf8);
        AlphaFadeValB = context.ReadFloat(0xfc);
        ScaleMode = context.ReadInt32(0x100);
        ScaleFadeStart = context.ReadFloat(0x104);
        ScaleFadeDuration = context.ReadFloat(0x108);
        ScaleFadeValA = context.ReadFloat(0x10c);
        ScaleFadeValB = context.ReadFloat(0x110);
        Acceleration = context.ReadFloat(0x114);
        ScalePerSecond = context.ReadFloat(0x118);
        TileAnimLoops = context.ReadInt32(0x11c);
        NumTilesX = context.ReadInt32(0x11c);
        NumTilesY = context.ReadInt32(0x11c);
        UpdateFlags = context.ReadInt32(0x11c);
        AlphaNoiseBase = context.ReadFloat(0x120);
        AlphaNoiseRange = context.ReadFloat(0x124);
        AlphaNoiseSpeed = context.ReadFloat(0x128);
        ScaleNoiseBase = context.ReadFloat(0x12c);
        ScaleNoiseRange = context.ReadFloat(0x130);
        ScaleNoiseSpeed = context.ReadFloat(0x134);
        RotOffsetX = context.ReadFloat(0x138);
        RotOffsetY = context.ReadFloat(0x13c);
        RenderBlendOp = context.ReadInt32(0x148);
        FaceMode = context.ReadInt32(0x148);
        AlphaRef = context.ReadInt32(0x148);
        BackfaceCulling = context.ReadInt32(0x148);
        MaterialMode = context.ReadInt32(0x148);
        UserDataFlags = context.ReadInt32(0x148);
        VelStretchMax = context.ReadFloat(0x14c);
        ColourAdd = context.ReadFloat4(0x150);
        ColourMul = context.ReadFloat4(0x160);
        ScreenRGBScX = context.ReadFloat(0x170);
        ScreenRGBScY = context.ReadFloat(0x174);
        SoftParticlesOffset = context.ReadFloat(0x178);
        SoftParticlesMultiply = context.ReadFloat(0x17c);
        ClipNearStart = context.ReadFloat(0x180);
        ClipNearDist = context.ReadFloat(0x184);
        ClipNFarStart = context.ReadFloat(0x188);
        ClipFarDist = context.ReadFloat(0x18c);
        ParticleRefractionMul = context.ReadFloat(0x190);
        LightAmbientMul = context.ReadFloat(0x194);
        LightMul = context.ReadFloat(0x198);
        MaxClipSize = context.ReadFloat(0x19c);
        CameraOffset = context.ReadFloat4(0x1a0);
        MaxParticlesPerContainer = context.ReadInt32(0x1b0);
        AffectorSets = context.ReadInt32(0x1b4);
        UVRectAsTiles = context.ReadBoolean(0x1b8);
        UVRectAsTilesDim = context.ReadInt32(0x1bc);
        Texture = (SeDefinitionTextureNode?)context.LoadNode(context.ReadInt32(0x1d0));
        TextureName = context.ReadStringPointer(0x1d4);
        SpawnEventEmitter = (SeDefinitionParticleEmitterNode?)context.LoadNode(context.ReadInt32(0x1fc));
        SpawnEventEmitterName = context.ReadStringPointer(0x200);
        ColourRamp = context.LoadNode(context.ReadInt32(0x228));
        ColourRampName = context.ReadStringPointer(0x22c);
        ScaleRamp = context.LoadNode(context.ReadInt32(0x24c));
        ScaleRampName = context.ReadStringPointer(0x258);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat4(buffer, Scale, 0x80);
        context.WriteFloat4(buffer, ScaleRandom, 0x90);
        context.WriteFloat4(buffer, Colour, 0xa0);
        context.WriteFloat4(buffer, ColourRandom, 0xb0);
        context.WriteFloat(buffer, Lifetime, 0xc0);
        context.WriteFloat(buffer, LifetimeRandom, 0xc4);
        context.WriteInt32(buffer, StyleFlags, 0xc8);
        context.WriteFloat4(buffer, UVTilesRect, 0xd0);
        context.WriteInt32(buffer, UVMode, 0xe0);
        context.WriteFloat(buffer, TileAnimStart, 0xe4);
        context.WriteFloat(buffer, TileAnimDuration, 0xe8);
        context.WriteInt32(buffer, AlphaMode, 0xec);
        context.WriteFloat(buffer, AlphaFadeStart, 0xf0);
        context.WriteFloat(buffer, AlphaFadeDuration, 0xf4);
        context.WriteFloat(buffer, AlphaFadeValA, 0xf8);
        context.WriteFloat(buffer, AlphaFadeValB, 0xfc);
        context.WriteInt32(buffer, ScaleMode, 0x100);
        context.WriteFloat(buffer, ScaleFadeStart, 0x104);
        context.WriteFloat(buffer, ScaleFadeDuration, 0x108);
        context.WriteFloat(buffer, ScaleFadeValA, 0x10c);
        context.WriteFloat(buffer, ScaleFadeValB, 0x110);
        context.WriteFloat(buffer, Acceleration, 0x114);
        context.WriteFloat(buffer, ScalePerSecond, 0x118);
        context.WriteInt32(buffer, TileAnimLoops, 0x11c);
        context.WriteInt32(buffer, NumTilesX, 0x11c);
        context.WriteInt32(buffer, NumTilesY, 0x11c);
        context.WriteInt32(buffer, UpdateFlags, 0x11c);
        context.WriteFloat(buffer, AlphaNoiseBase, 0x120);
        context.WriteFloat(buffer, AlphaNoiseRange, 0x124);
        context.WriteFloat(buffer, AlphaNoiseSpeed, 0x128);
        context.WriteFloat(buffer, ScaleNoiseBase, 0x12c);
        context.WriteFloat(buffer, ScaleNoiseRange, 0x130);
        context.WriteFloat(buffer, ScaleNoiseSpeed, 0x134);
        context.WriteFloat(buffer, RotOffsetX, 0x138);
        context.WriteFloat(buffer, RotOffsetY, 0x13c);
        context.WriteInt32(buffer, RenderBlendOp, 0x148);
        context.WriteInt32(buffer, FaceMode, 0x148);
        context.WriteInt32(buffer, AlphaRef, 0x148);
        context.WriteInt32(buffer, BackfaceCulling, 0x148);
        context.WriteInt32(buffer, MaterialMode, 0x148);
        context.WriteInt32(buffer, UserDataFlags, 0x148);
        context.WriteFloat(buffer, VelStretchMax, 0x14c);
        context.WriteFloat4(buffer, ColourAdd, 0x150);
        context.WriteFloat4(buffer, ColourMul, 0x160);
        context.WriteFloat(buffer, ScreenRGBScX, 0x170);
        context.WriteFloat(buffer, ScreenRGBScY, 0x174);
        context.WriteFloat(buffer, SoftParticlesOffset, 0x178);
        context.WriteFloat(buffer, SoftParticlesMultiply, 0x17c);
        context.WriteFloat(buffer, ClipNearStart, 0x180);
        context.WriteFloat(buffer, ClipNearDist, 0x184);
        context.WriteFloat(buffer, ClipNFarStart, 0x188);
        context.WriteFloat(buffer, ClipFarDist, 0x18c);
        context.WriteFloat(buffer, ParticleRefractionMul, 0x190);
        context.WriteFloat(buffer, LightAmbientMul, 0x194);
        context.WriteFloat(buffer, LightMul, 0x198);
        context.WriteFloat(buffer, MaxClipSize, 0x19c);
        context.WriteFloat4(buffer, CameraOffset, 0x1a0);
        context.WriteInt32(buffer, MaxParticlesPerContainer, 0x1b0);
        context.WriteInt32(buffer, AffectorSets, 0x1b4);
        context.WriteBoolean(buffer, UVRectAsTiles, 0x1b8);
        context.WriteInt32(buffer, UVRectAsTilesDim, 0x1bc);
        context.WriteInt32(buffer, Texture?.Uid ?? 0, 0x1d0);
        context.WriteStringPointer(buffer, TextureName, 0x1d4);
        context.WriteInt32(buffer, SpawnEventEmitter?.Uid ?? 0, 0x1fc);
        context.WriteStringPointer(buffer, SpawnEventEmitterName, 0x200);
        context.WriteInt32(buffer, ColourRamp?.Uid ?? 0, 0x228);
        context.WriteStringPointer(buffer, ColourRampName, 0x22c);
        context.WriteInt32(buffer, ScaleRamp?.Uid ?? 0, 0x254);
        context.WriteStringPointer(buffer, ScaleRampName, 0x258);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x280;
}

// public enum UVModeEnumSet
// {
// 	STATIC = 0,
// 	RANDOM = 1,
// 	PERFRAMERANDOM = 2,
// 	LIFETIME = 3,
// 	BOUNCE = 4,
//
// }
// public enum Alpha_ModeEnumSet
// {
// 	NONE = 0,
// 	LINEARIN = 1,
// 	LINEAROUT = 2,
// 	SCURVEIN = 3,
// 	SCURVEOUT = 4,
// 	WAVEINOUT = 5,
// 	WAVEOUTIN = 6,
// 	ZEROABZERO = 7,
//
// }
// public enum Scale_ModeEnumSet
// {
// 	NONE = 0,
// 	LINEARIN = 1,
// 	LINEAROUT = 2,
// 	SCURVEIN = 3,
// 	SCURVEOUT = 4,
// 	WAVEINOUT = 5,
// 	WAVEOUTIN = 6,
// 	ZEROABZERO = 7,
//
// }
// public enum Render_Blend_OpEnumSet
// {
// 	MOD = 0,
// 	ADD = 1,
// 	SUB = 2,
// 	MUL = 3,
// 	MOD_ALPHA_ADD = 4,
// 	SRC = 5,
//
// }
// public enum FaceModeEnumSet
// {
// 	CAMERA = 0,
// 	VELOCITY = 1,
// 	FLAT_XZ = 2,
// 	LOCK_Y = 3,
// 	PLANE_VEL = 4,
//
// }
// public enum Backface_CullingEnumSet
// {
// 	NORMAL = 0,
// 	INVERTED = 1,
// 	OFF = 2,
//
// }
// public enum MaterialModeEnumSet
// {
// 	Plain = 0,
// 	ScreenRGB = 1,
// 	SoftParticles = 2,
// 	Refraction = 3,
//
// }
// public enum TextureEnumSet
// {
// 	SeDefinitionTextureNode = 93477184,
//
// }
// public enum SpawnEventEmitterEnumSet
// {
// 	SeDefinitionParticleEmitterNode = 93477568,
//
// }
// public enum ColourRampEnumSet
// {
// 	SeDefinitionRampNodeVector4 = 93476896,
//
// }
// public enum ScaleRampEnumSet
// {
// 	SeDefinitionRampNodeVector4 = 1994748133,
//
// }
