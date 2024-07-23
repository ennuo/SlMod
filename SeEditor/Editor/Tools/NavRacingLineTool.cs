using System.Numerics;
using SeEditor.Editor.Attributes;
using SeEditor.Renderer;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;

namespace SeEditor.Editor.Tools;

public class NavRacingLineTool(Navigation navigation) : EditorTool
{
    private static readonly Vector3 MarkerColor = new(209.0f / 255.0f, 209.0f / 255.0f, 14.0f / 255.0f);
    private static readonly Vector3 CrossSectionColor = new(14.0f / 255.0f, 14.0f / 255.0f, 228.0f / 255.0f);
    private static readonly Vector3 LinkColor = new(215.0f / 255.0f, 14.0f / 255.0f, 255.0f / 255.0f);
    
    private Navigation _navData = navigation;
    
    public override void UpdateUI()
    {
        
    }
    
    public override void Render()
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
                        
                        
            LineRenderPrimitives.DrawLine(prev.RacingLine, next.RacingLine, Vector3.One);
                        
            LineRenderPrimitives.DrawLine(prevLink.Left, nextLink.Left, LinkColor);
            LineRenderPrimitives.DrawLine(prevLink.Right, nextLink.Right, LinkColor);
                        
            LineRenderPrimitives.DrawLine(prevLink.Left, prevLink.Right, CrossSectionColor);
                        
            LineRenderPrimitives.DrawLine(prevLink.From!.Pos, prevLink.To!.Pos, LinkColor);
                        
            LineRenderPrimitives.DrawLine(prevLink.From!.Pos, prevLink.From!.Pos + prevLink.From!.Up * 4.0f, MarkerColor);
        }
    }
}