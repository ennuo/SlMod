using SlLib.Resources.Database;

namespace SlLib.Serialization;

public interface ISumoResource : ILoadable
{
    public SlResourceHeader Header { get; set; }
}