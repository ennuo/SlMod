using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public abstract class SeDefinitionTimeLineEventBaseNode : SeDefinitionNode
{
    public float Start;
    public float Duration;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Start = context.ReadFloat(0x80);
        Duration = context.ReadFloat(0x84);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Start, 0x80);
        context.WriteFloat(buffer, Duration, 0x84);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x90;
}