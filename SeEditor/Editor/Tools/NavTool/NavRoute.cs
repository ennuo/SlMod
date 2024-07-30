using SlLib.SumoTool.Siff.NavData;

namespace SeEditor.Editor.Tools.NavTool;

public class NavRoute(int id)
{
    public string Name = $"Route {id}";
    public int Id = id;
    public List<NavWaypoint> Waypoints = [];
}