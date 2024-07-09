using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Definitions;

public abstract class ShaderDefinitionBaseNode : SeDefinitionNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x80;
}