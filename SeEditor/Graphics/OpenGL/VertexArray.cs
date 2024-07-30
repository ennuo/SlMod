using OpenTK.Graphics.OpenGL;

namespace SeEditor.Graphics.OpenGL;

public class VertexArray
{
    private readonly int _id = GL.GenVertexArray();

    ~VertexArray()
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