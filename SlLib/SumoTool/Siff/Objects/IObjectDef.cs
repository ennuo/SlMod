using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public interface IObjectDef : IResourceSerializable
{
    public string ObjectType { get; }
}