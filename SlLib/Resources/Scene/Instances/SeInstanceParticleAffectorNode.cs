using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceParticleAffectorNode : SeInstanceTransformNode
{
    public float ForceMultiplier;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        ForceMultiplier = context.ReadFloat(0x168);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, ForceMultiplier, 0x168);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x180;
}