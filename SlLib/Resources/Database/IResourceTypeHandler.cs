using SlLib.Serialization;

namespace SlLib.Resources.Database;

public interface IResourceTypeHandler
{
    void Install(ISumoResource resource);
    void Uninstall(ISumoResource resource);
}