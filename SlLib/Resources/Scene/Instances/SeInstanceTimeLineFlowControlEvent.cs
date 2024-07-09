using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceTimeLineFlowControlEvent : SeInstanceTimeLineEventNodeBase
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xa4;
}