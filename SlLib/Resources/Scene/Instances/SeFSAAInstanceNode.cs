using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

// ReSharper disable once InconsistentNaming
public class SeFSAAInstanceNode : SeInstanceNode
{
    public int FSAASharpnessId;
    public float FSAASharpnessScale;
    public int FSAAModel;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        FSAASharpnessId = context.ReadInt32(0x88);
        FSAASharpnessScale = context.ReadFloat(0x8c);
        FSAAModel = context.ReadInt32(0x90);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, FSAASharpnessId, 0x88);
        context.WriteFloat(buffer, FSAASharpnessScale, 0x8c);
        context.WriteInt32(buffer, FSAAModel, 0x90);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x98;
}