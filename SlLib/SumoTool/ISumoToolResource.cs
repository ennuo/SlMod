using SlLib.Serialization;

namespace SlLib.SumoTool;

public interface ISumoToolResource : ILoadable, IWritable
{
    public SiffResourceType Type { get; }
}