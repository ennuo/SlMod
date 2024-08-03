using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace SeEditor.Graphics.OpenGL;

public class UniformBuffer
{
    private int _id;
    
    public UniformBuffer(int size, int binding)
    {
        _id = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, _id);
        GL.BufferData(BufferTarget.UniformBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, binding, _id);
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }
    
    ~UniformBuffer()
    {
        GL.DeleteBuffer(_id);
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, _id);
    }

    public void Unbind()
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }

    public unsafe void SetData(ArraySegment<byte> data)
    {
        if (data.Count == 0) return;

        Bind();
        fixed (byte* b = &data.Array![data.Offset])
        {
            GL.BufferSubData(BufferTarget.UniformBuffer, 0, data.Count, (nint)b);
        }
        Unbind();
    }

    public void SetData<T>(T data) where T : struct
    {
        Bind();
        GL.BufferSubData(BufferTarget.UniformBuffer, 0, Marshal.SizeOf<T>(), ref data);
        Unbind();
    }
}