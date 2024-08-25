using System.Numerics;
using SeEditor.Renderer;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;

namespace SeEditor.Editor.Tools;

public class NavigationTool(Navigation navigation) : EditorTool
{
    private static readonly Vector3 MarkerColor = new(209.0f / 255.0f, 209.0f / 255.0f, 14.0f / 255.0f);
    private static readonly Vector3 CrossSectionColor = new(14.0f / 255.0f, 14.0f / 255.0f, 228.0f / 255.0f);
    private static readonly Vector3 LinkColor = new(215.0f / 255.0f, 14.0f / 255.0f, 255.0f / 255.0f);
    
    private Navigation _navData = navigation;
    
    public override void OnRender()
    {
        if (_navData.RacingLines.Count > 0)
            RenderRacingLine(0);
    }
    
    public void RenderRacingLine(int index)
    {
        NavRacingLine line = _navData.RacingLines[index];
        for (int i = 1; i < line.Segments.Count + 1; ++i)
        {
            NavRacingLineSeg prev = line.Segments[i - 1];
            NavRacingLineSeg next = line.Segments[i % line.Segments.Count];
                        
            NavWaypointLink prevLink = prev.Link!;
            NavWaypointLink nextLink = next.Link!;
                        
                        
            PrimitiveRenderer.DrawLine(prev.RacingLine, next.RacingLine, Vector3.One);
                        
            PrimitiveRenderer.DrawLine(prevLink.Left, nextLink.Left, LinkColor);
            PrimitiveRenderer.DrawLine(prevLink.Right, nextLink.Right, LinkColor);
                        
            PrimitiveRenderer.DrawLine(prevLink.Left, prevLink.Right, CrossSectionColor);
                        
            PrimitiveRenderer.DrawLine(prevLink.From!.Pos, prevLink.To!.Pos, LinkColor);
                        
            PrimitiveRenderer.DrawLine(prevLink.From!.Pos, prevLink.From!.Pos + prevLink.From!.Up * 4.0f, MarkerColor);
        }
    }
}