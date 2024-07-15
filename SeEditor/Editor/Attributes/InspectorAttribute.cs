namespace SeEditor.Editor.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class InspectorAttribute(string header) : Attribute
{
    public string Header => header;
}