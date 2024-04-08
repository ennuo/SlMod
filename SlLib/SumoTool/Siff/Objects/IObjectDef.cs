using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public interface IObjectDef : ILoadable
{
    public string ObjectType { get; }
}