using System.Numerics;
using OpenTK.Graphics.OpenGL;

namespace SeEditor.Graphics.OpenGL;

public class Shader
{
    private readonly int _id;
    private Dictionary<string, int> _locations = [];
    
    public Shader(string vertexfp, string fragmentfp)
    {
        if (!File.Exists(vertexfp))
            throw new FileNotFoundException($"Could not find vertex shader source: {vertexfp}");
        if (!File.Exists(fragmentfp))
            throw new FileNotFoundException($"Could not find fragment shader source: {fragmentfp}");

        int vertex = CompileShader(ShaderType.VertexShader, File.ReadAllText(vertexfp));
        int fragment = CompileShader(ShaderType.FragmentShader, File.ReadAllText(fragmentfp));
        
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
        {
            string log = GL.GetProgramInfoLog(program);
            throw new Exception($"GL.LinkProgram failed! Log:\n{log}");
        }
        
        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        _id = program;
        
        // Cache the uniform locations
        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int numUniforms);
        for (int i = 0; i < numUniforms; ++i)
        {
            string name = GL.GetActiveUniformName(program, i);
            _locations[name] = GL.GetUniformLocation(program, name);
        }
        
        GL.GetProgram(program, GetProgramParameterName.ActiveUniformBlocks, out int numBlocks);
        for (int i = 0; i < numBlocks; ++i)
        {
            string name = GL.GetActiveUniformBlockName(program, i);
            Console.WriteLine("uniform block: " + name);
            
        }
    }
    
    ~Shader()
    {
        if (_id != 0) 
            GL.DeleteProgram(_id);
    }
    
    public unsafe void SetMatrix4(string name, ref Matrix4x4 matrix)
    {
        int location = _locations[name];
        fixed (Matrix4x4* m = &matrix)
        {
            GL.UniformMatrix4(location, 1, false, (float*)m);
        }
    }

    public unsafe void SetVector3(string name, ref Vector4 v)
    {
        int location = _locations[name];
        fixed (Vector4* m = &v)
        {
            GL.Uniform3(location, 1, (float*)m);
        }
    }
    
    public unsafe void SetVector3(string name, ref Vector3 v)
    {
        int location = _locations[name];
        fixed (Vector3* m = &v)
        {
            GL.Uniform3(location, 1, (float*)m);
        }
    }

    public void SetInt(string name, int v)
    {
        int location = _locations[name];
        GL.Uniform1(location, v);
    }
    
    public void Bind()
    {
        GL.UseProgram(_id);
    }

    public void Unbind()
    {
        GL.UseProgram(0);
    }
    
    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            throw new Exception($"GL.CompileShader failed! Log:\n{info}");
        }

        return shader;
    }
    
}