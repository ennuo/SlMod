using System.Numerics;
using SeEditor.Renderer;
using SlLib.Extensions;
using SlLib.Resources.Scene.Instances;

namespace SeEditor.Editor.Tools;

public class SplineTool : EditorTool
{
    public override void OnRender()
    {
        if (Target is not SeInstanceSplineNode spline) return;
        
        Vector3 pivot = spline.WorldMatrix.Translation;
        PrimitiveRenderer.BeginPrimitiveScene();
        
        for (int i = 0; i < spline.Data.Count; i += 0x40)
        {
            Vector3 pos = pivot + spline.Data.ReadFloat3(i + 0x30);
            Vector3 next = pivot + spline.Data.ReadFloat3(((i + 0x40) % spline.Data.Count) + 0x30);
            PrimitiveRenderer.DrawLine(pos, next, Vector3.One);
        }
        
        PrimitiveRenderer.EndPrimitiveScene();
    }
}