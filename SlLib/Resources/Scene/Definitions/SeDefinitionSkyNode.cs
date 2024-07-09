using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionSkyNode : SeDefinitionEntityNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xe0;
}