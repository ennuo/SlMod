using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Logic;

public class Locator : IResourceSerializable
{
    public int GroupNameHash;
    public int LocatorNameHash;
    public int MeshForestNameHash;
    public int MeshTreeNameHash;
    public int SetupObjectNameHash;
    public int AnimatedInstanceNameHash;
    public int SubDataHash;
    public int Flags;
    public int Health;
    public float SequenceStartFrameMultiplier;
    public float SequencerInterSpawnMultiplier;
    public float AnimatedInstancePlaybackSpeed;
    public Vector4 PositionAsFloats;
    public Vector4 RotationAsFloats;
    
    public void Load(ResourceLoadContext context)
    {
        GroupNameHash = context.ReadInt32();
        LocatorNameHash = context.ReadInt32();
        MeshForestNameHash = context.ReadInt32();
        MeshTreeNameHash = context.ReadInt32();
        SetupObjectNameHash = context.ReadInt32();
        AnimatedInstanceNameHash = context.ReadInt32();
        SubDataHash = context.ReadInt32();
        Flags = context.ReadInt32();
        Health = context.ReadInt32();
        SequenceStartFrameMultiplier = context.ReadFloat();
        SequencerInterSpawnMultiplier = context.ReadFloat();
        AnimatedInstancePlaybackSpeed = context.ReadFloat();
        PositionAsFloats = context.ReadFloat4();
        RotationAsFloats = context.ReadFloat4();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, GroupNameHash, 0x0);
        context.WriteInt32(buffer, LocatorNameHash, 0x4);
        context.WriteInt32(buffer, MeshForestNameHash, 0x8);
        context.WriteInt32(buffer, MeshTreeNameHash, 0xc);
        context.WriteInt32(buffer, SetupObjectNameHash, 0x10);
        context.WriteInt32(buffer, AnimatedInstanceNameHash, 0x14);
        context.WriteInt32(buffer, SubDataHash, 0x18);
        context.WriteInt32(buffer, Flags, 0x1c);
        context.WriteInt32(buffer, Health, 0x20);
        context.WriteFloat(buffer, SequenceStartFrameMultiplier, 0x24);
        context.WriteFloat(buffer, SequencerInterSpawnMultiplier, 0x28);
        context.WriteFloat(buffer, AnimatedInstancePlaybackSpeed, 0x2c);
        context.WriteFloat4(buffer, PositionAsFloats, 0x30);
        context.WriteFloat4(buffer, RotationAsFloats, 0x40);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x50;
    }
}