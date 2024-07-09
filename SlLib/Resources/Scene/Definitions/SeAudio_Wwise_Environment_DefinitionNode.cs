using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeAudio_Wwise_Environment_DefinitionNode : SeDefinitionTransformNode
{
    public string EnvName;
    public int VolumeRTCP;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        EnvName = context.ReadStringPointer(0xd4);
        VolumeRTCP = context.ReadInt32(0xf4);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteStringPointer(buffer, EnvName, 0xd4);
        context.WriteInt32(buffer, VolumeRTCP, 0xf4);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x100;
}