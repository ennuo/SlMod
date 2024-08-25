namespace SlLib.SumoTool.Siff.Entry;

public class Scene(int hash)
{
    public int Hash = hash;
    public List<int> Objects = [];
    public int WidescreenFlag;
    public int Index;
    public float Width = 1280.0f;
    public float Height = 720.0f;
}