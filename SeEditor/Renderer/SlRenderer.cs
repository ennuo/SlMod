using OpenTK.Graphics.OpenGL;
using SeEditor.Editor;
using SeEditor.Graphics.OpenGL;
using SeEditor.Renderer.Buffers;

namespace SeEditor.Renderer;

public class SlRenderer
{
    public EditorFramebuffer Framebuffer;
    public UniformBuffer[] RenderBuffers = new UniformBuffer[SlRenderBuffers.Count];
    
    public SlRenderer(int width, int height)
    {
        Framebuffer = new EditorFramebuffer(width, height);
    }
}