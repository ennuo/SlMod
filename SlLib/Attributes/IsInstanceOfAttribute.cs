namespace SlLib.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class IsInstanceOfAttribute(Type type) : Attribute
{
    public Type Type = type;
}