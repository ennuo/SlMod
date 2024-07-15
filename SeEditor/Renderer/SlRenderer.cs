using OpenTK.Graphics.OpenGL;
using SeEditor.Renderer.Buffers;

namespace SeEditor.Renderer;

public class SlRenderer
{
    public EditorFramebuffer Framebuffer;
    public ConstantBufferViewProjection ViewProjectionBuffer;
    public ConstantBufferWorldMatrix WorldMatrixBuffer;
    private int[] _buffers = new int[SlRenderBuffers.Count];
    
    public SlRenderer(int width, int height)
    {
        Framebuffer = new EditorFramebuffer(width, height);
        CreateUniformBuffer(SlRenderBuffers.WorldMatrix, 0x40);
        CreateUniformBuffer(SlRenderBuffers.ViewProjection, 0x100);
    }
    
    private void CreateUniformBuffer(int index, int size)
    {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, buffer);
        GL.BufferData(BufferTarget.UniformBuffer, size, IntPtr.Zero, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        _buffers[index] = buffer;
    }
}