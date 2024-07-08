using OpenTK.Graphics.OpenGL;

namespace SeEditor.Graphics.OpenGL;

public class GlVertexArray
{
    private readonly int _id = GL.GenVertexArray();

    ~GlVertexArray()
    {
        GL.DeleteVertexArray(_id);
    }

    public void Bind()
    {
        GL.BindVertexArray(_id);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }
}