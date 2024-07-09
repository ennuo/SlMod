using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeGiInstanceCameraVolume : SeInstanceTransformNode
{
    public float Width;
    public float Height;
    public float Depth;
    public int MetersPerVoxel;
    public Vector4 GiDuffuseColourMul;
    public Vector4 GiSpecularColourMul;
    public float GiDiffuseIntensityMul;
    public float GiSpecularIntensityMul;
    public float GiSpecularPowerMul;
    public bool EnableGILighing;
    public bool ShowGiOnly;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Width = context.ReadFloat(0x160);
        Height = context.ReadFloat(0x164);
        Depth = context.ReadFloat(0x168);
        MetersPerVoxel = context.ReadInt32(0x178);
        GiDuffuseColourMul = context.ReadFloat4(0x1e0);
        GiSpecularColourMul = context.ReadFloat4(0x1f0);
        GiDiffuseIntensityMul = context.ReadFloat(0x204);
        GiSpecularIntensityMul = context.ReadFloat(0x208);
        GiSpecularPowerMul = context.ReadFloat(0x20c);
        EnableGILighing = context.ReadBoolean(0x228, wide: true);
        ShowGiOnly = context.ReadBoolean(0x22c, wide: true);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Width, 0x160);
        context.WriteFloat(buffer, Height, 0x164);
        context.WriteFloat(buffer, Depth, 0x168);
        context.WriteInt32(buffer, MetersPerVoxel, 0x178);
        context.WriteFloat4(buffer, GiDuffuseColourMul, 0x1e0);
        context.WriteFloat4(buffer, GiSpecularColourMul, 0x1f0);
        context.WriteFloat(buffer, GiDiffuseIntensityMul, 0x204);
        context.WriteFloat(buffer, GiSpecularIntensityMul, 0x208);
        context.WriteFloat(buffer, GiSpecularPowerMul, 0x20c);
        context.WriteBoolean(buffer, EnableGILighing, 0x228, wide: true);
        context.WriteBoolean(buffer, ShowGiOnly, 0x22c, wide: true);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x230;
}
