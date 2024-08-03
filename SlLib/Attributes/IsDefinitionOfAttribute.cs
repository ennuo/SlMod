namespace SlLib.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class IsDefinitionOfAttribute(Type type) : Attribute
{
    public Type Type = type;
}