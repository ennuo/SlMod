using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public interface IObjectDef : IResourceSerializable
{
    public string ObjectType { get; }
    public Vector2 Anchor { get; set; }
    public int Layer { get; set; }
}