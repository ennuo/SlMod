using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Definitions;

public class SeToneMappingRefDefinitionNode : SeDefinitionNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x80;
}