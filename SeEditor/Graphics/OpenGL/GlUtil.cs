using System.Numerics;
using OpenTK.Graphics.OpenGL;

namespace SeEditor.Graphics.OpenGL;

public static class GlUtil
{
    public static unsafe void UniformMatrix4(int location, ref Matrix4x4 matrix)
    {
        fixed (Matrix4x4* m = &matrix)
        {
            GL.UniformMatrix4(location, 1, false, (float*)m);
        }
    }

    public static unsafe void UniformVector3(int location, ref Vector4 v)
    {
        fixed (Vector4* m = &v)
        {
            GL.Uniform3(location, 1, (float*)m);
        }
    }

    public static unsafe void UniformVector3(int location, ref Vector3 v)
    {
        fixed (Vector3* m = &v)
        {
            GL.Uniform3(location, 1, (float*)m);
        }
    }
    
    public static unsafe void UniformMatrix4Array(int location, Matrix4x4[] matrices)
    {
        fixed (Matrix4x4* m = matrices)
        {
            GL.UniformMatrix4(location, matrices.Length, false, (float*)m);
        }
    }
}