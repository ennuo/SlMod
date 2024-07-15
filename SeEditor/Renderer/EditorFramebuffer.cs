using OpenTK.Graphics.OpenGL;

namespace SeEditor.Renderer;

public class EditorFramebuffer
{
    private int _id;
    private int _width, _height;
    private List<int> _attachments;
    private int _rbo;

    public EditorFramebuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _id = GL.GenFramebuffer();
        
        _attachments = [GL.GenTexture(), GL.GenTexture()];
        _rbo = GL.GenRenderbuffer();
        
        Invalidate();
    }

    private void Invalidate()
    {
        int colorAttachment = _attachments[0];
        int pickerAttachment = _attachments[1];
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);
        
        // Setup the primary color attachment
        {
            GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, _width, _height, 0,
                PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, colorAttachment, 0);    
        }
        
        // Setup the secondary attachment for picking
        {
            GL.BindTexture(TextureTarget.Texture2D, pickerAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, _width, _height, 0,
                PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2D, pickerAttachment, 0);    
        }
        
        
        
        GL.DrawBuffers(2, [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1]);
        
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, _width, _height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public int GetColorAttachment(int index) => _attachments[index];
    
    public int GetRenderTexture() => _attachments[0];
    public int GetPickerTexture() => _attachments[1];

    public int GetEntityPick(int x, int y)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
        int entity = 0;
        GL.ReadPixels(x, y, 1, 1, PixelFormat.RedInteger, PixelType.Int, ref entity);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return entity;
    }
    
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        Invalidate();
    }
    
    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);
        GL.Viewport(0, 0, _width, _height);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}