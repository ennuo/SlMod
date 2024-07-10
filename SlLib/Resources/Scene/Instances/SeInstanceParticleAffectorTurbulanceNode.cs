using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceParticleAffectorTurbulanceNode : SeInstanceParticleAffectorNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x180;
}