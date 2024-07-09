using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Definitions;

public class SeAudio_Wwise_Event_Volume_DefinitionNode : SeAudio_Wwise_Event_DefinitionNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x130;
}