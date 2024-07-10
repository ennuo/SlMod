using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionTimeLinePerformAction : SeDefinitionTimeLineEventBaseNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x90;
}