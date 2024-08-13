using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Keyframe;

public class KeyframeData : IResourceSerializable
{
    public int Hash = SlUtil.SumoHash("KEYFRAME_0");
    public int FrameNumber;
    public byte R = 255, G = 255, B = 255, A = 255;
    public float X, Y;
    public Vector2 Scale = Vector2.One;
    public short Priority;
    public short IsOn = 1;
    public float Rotation;
    public float Z;
    
    public virtual void Load(ResourceLoadContext context)
    {
        Hash = context.ReadInt32();
        FrameNumber = context.ReadInt32();
        R = context.ReadInt8();
        G = context.ReadInt8();
        B = context.ReadInt8();
        A = context.ReadInt8();
        X = context.ReadFloat();
        Y = context.ReadFloat();
        Scale = context.ReadFloat2();
        Priority = context.ReadInt16();
        IsOn = context.ReadInt16();
        Rotation = context.ReadFloat();
        Z = context.ReadFloat();
    }
    
    public virtual void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Hash, 0x0);
        context.WriteInt32(buffer, FrameNumber, 0x4);
        context.WriteInt8(buffer, R, 0x8);
        context.WriteInt8(buffer, G, 0x9);
        context.WriteInt8(buffer, B, 0xa);
        context.WriteInt8(buffer, A, 0xb);
        context.WriteFloat(buffer, X, 0xc);
        context.WriteFloat(buffer, Y, 0x10);
        context.WriteFloat2(buffer, Scale, 0x14);
        context.WriteInt16(buffer, Priority, 0x1c);
        context.WriteInt16(buffer, IsOn, 0x1e);
        context.WriteFloat(buffer, Rotation, 0x20);
        context.WriteFloat(buffer, Z, 0x24);
    }
    
    public virtual int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28;
    }
}