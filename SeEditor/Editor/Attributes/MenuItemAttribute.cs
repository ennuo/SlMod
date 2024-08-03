namespace SeEditor.Editor.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class MenuItemAttribute(string path) : Attribute
{
    public string Path = path;
}