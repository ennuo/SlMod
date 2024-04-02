namespace SlLib.Serialization;

public interface ILoadable
{
    void Load(ResourceLoadContext context, int offset);
}