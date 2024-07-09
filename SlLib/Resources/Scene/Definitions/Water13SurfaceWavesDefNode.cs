using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class Water13SurfaceWavesDefNode : SeDefinitionNode
{
    public float Wavelength0;
    public float Direction0;
    public float Amplitude0;
    public float Speed0;
    public int Steepness0;
    public bool Enabled0;
    public float Wavelength1;
    public float Direction1;
    public float Amplitude1;
    public float Speed1;
    public int Steepness1;
    public bool Enabled1;
    public float Wavelength2;
    public float Direction2;
    public float Amplitude2;
    public float Speed2;
    public int Steepness2;
    public bool Enabled2;
    public float Wavelength3;
    public float Direction3;
    public float Amplitude3;
    public float Speed3;
    public int Steepness3;
    public bool Enabled3;
    public int Version;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Wavelength0 = context.ReadFloat(0x80);
        Direction0 = context.ReadFloat(0x84);
        Amplitude0 = context.ReadFloat(0x88);
        Speed0 = context.ReadFloat(0x8c);
        Steepness0 = context.ReadInt32(0x90);
        Enabled0 = context.ReadBoolean(0x94, wide: true);
        Wavelength1 = context.ReadFloat(0x98);
        Direction1 = context.ReadFloat(0x9c);
        Amplitude1 = context.ReadFloat(0xa0);
        Speed1 = context.ReadFloat(0xa4);
        Steepness1 = context.ReadInt32(0xa8);
        Enabled1 = context.ReadBoolean(0xac, wide: true);
        Wavelength2 = context.ReadFloat(0xb0);
        Direction2 = context.ReadFloat(0xb4);
        Amplitude2 = context.ReadFloat(0xb8);
        Speed2 = context.ReadFloat(0xbc);
        Steepness2 = context.ReadInt32(0xc0);
        Enabled2 = context.ReadBoolean(0xc4, wide: true);
        Wavelength3 = context.ReadFloat(0xc8);
        Direction3 = context.ReadFloat(0xcc);
        Amplitude3 = context.ReadFloat(0xd0);
        Speed3 = context.ReadFloat(0xd4);
        Steepness3 = context.ReadInt32(0xd8);
        Enabled3 = context.ReadBoolean(0xdc, wide: true);
        Version = context.ReadInt32(0x140);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Wavelength0, 0x80);
        context.WriteFloat(buffer, Direction0, 0x84);
        context.WriteFloat(buffer, Amplitude0, 0x88);
        context.WriteFloat(buffer, Speed0, 0x8c);
        context.WriteInt32(buffer, Steepness0, 0x90);
        context.WriteBoolean(buffer, Enabled0, 0x94, wide: true);
        context.WriteFloat(buffer, Wavelength1, 0x98);
        context.WriteFloat(buffer, Direction1, 0x9c);
        context.WriteFloat(buffer, Amplitude1, 0xa0);
        context.WriteFloat(buffer, Speed1, 0xa4);
        context.WriteInt32(buffer, Steepness1, 0xa8);
        context.WriteBoolean(buffer, Enabled1, 0xac, wide: true);
        context.WriteFloat(buffer, Wavelength2, 0xb0);
        context.WriteFloat(buffer, Direction2, 0xb4);
        context.WriteFloat(buffer, Amplitude2, 0xb8);
        context.WriteFloat(buffer, Speed2, 0xbc);
        context.WriteInt32(buffer, Steepness2, 0xc0);
        context.WriteBoolean(buffer, Enabled2, 0xc4, wide: true);
        context.WriteFloat(buffer, Wavelength3, 0xc8);
        context.WriteFloat(buffer, Direction3, 0xcc);
        context.WriteFloat(buffer, Amplitude3, 0xd0);
        context.WriteFloat(buffer, Speed3, 0xd4);
        context.WriteInt32(buffer, Steepness3, 0xd8);
        context.WriteBoolean(buffer, Enabled3, 0xdc, wide: true);
        context.WriteInt32(buffer, Version, 0x140);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x150;
}
