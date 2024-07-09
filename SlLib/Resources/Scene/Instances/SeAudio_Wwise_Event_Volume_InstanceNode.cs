using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeAudio_Wwise_Event_Volume_InstanceNode : SeAudio_Wwise_Event_InstanceNode
{
    public int VolumeShape;
    public float MinimumDistanceFromListener;
    public float Radius;
    public int Pad0;
    public Vector4 HalfBounds;
    public Vector4 EndPoint1;
    public Vector4 EndPoint2;
    public Matrix4x4 WorldToLocal;
    public Matrix4x4 EmitterMat;
    public int[] Pad2 = new int[2];

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        VolumeShape = context.ReadInt32(0x190);
        MinimumDistanceFromListener = context.ReadFloat(0x194);
        Radius = context.ReadFloat(0x198);
        Pad0 = context.ReadInt32(0x19c);
        HalfBounds = context.ReadFloat4(0x1a0);
        EndPoint1 = context.ReadFloat4(0x1b0);
        EndPoint2 = context.ReadFloat4(0x1c0);
        WorldToLocal = context.ReadMatrix(0x1d0);
        EmitterMat = context.ReadMatrix(0x210);
        Pad2[0] = context.ReadInt32(0x258);
        Pad2[1] = context.ReadInt32(0x25c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, VolumeShape, 0x190);
        context.WriteFloat(buffer, MinimumDistanceFromListener, 0x194);
        context.WriteFloat(buffer, Radius, 0x198);
        context.WriteInt32(buffer, Pad0, 0x19c);
        context.WriteFloat4(buffer, HalfBounds, 0x1a0);
        context.WriteFloat4(buffer, EndPoint1, 0x1b0);
        context.WriteFloat4(buffer, EndPoint2, 0x1c0);
        context.WriteMatrix(buffer, WorldToLocal, 0x1d0);
        context.WriteMatrix(buffer, EmitterMat, 0x210);
        context.WriteInt32(buffer, Pad2[0], 0x258);
        context.WriteInt32(buffer, Pad2[1], 0x25c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x260;
}

// public enum VolumeShapeEnumSet
// {
// 	Sphere = 0,
// 	Box = 1,
// 	Line = 2,
//
// }
