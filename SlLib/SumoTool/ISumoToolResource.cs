using SlLib.Serialization;

namespace SlLib.SumoTool;

public interface ISumoToolResource : IResourceSerializable
{
    public SiffResourceType Type { get; }
}