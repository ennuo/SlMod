using OpenTK.Graphics.OpenGL;
using SeEditor.Renderer;
using SlLib.Resources;
using Buffer = System.Buffer;

namespace SeEditor.Installers;

public static class SlTextureInstaller
{
    public static void Install(SlTexture texture)
    {
        if (texture.ID != 0 || !texture.HasData()) return;
        texture.ID = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture.ID);

        bool hasMipMaps = texture.Mips > 1;
        if (hasMipMaps)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, texture.Mips - 1);
        }


        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        int numFaces = texture.Cubemap ? 6 : 1;

        if ((int)texture.Format >= SlTexturePlatformInfo.Info.Length) return;
        var info = SlTexturePlatformInfo.Info[(int)texture.Format];
        if (!info.IsValid())
        {
            Console.WriteLine($"UNSUPPORTED TEXTURE FORMAT: {texture.Header.Name} : {texture.Format}");
            return;
        }
        
        bool isCompressedTexture = info.IsCompressedType();

        int dataOffset = 0x80;
        int width = texture.Width, height = texture.Height;

        for (int face = 0; face < numFaces; ++face)
        for (int i = 0; i < texture.Mips; ++i)
        {
            var target = TextureTarget.Texture2D;
            if (texture.Cubemap) target = TextureTarget.TextureCubeMapPositiveX + face;

            int size;
            if (isCompressedTexture)
                size = ((width + 3) / 4) * ((height + 3) / 4) * (info.Stride * 2);
            else
                size = width * height * ((info.Stride + 7) / 8);

            byte[] textureData = new byte[size];
            Buffer.BlockCopy(texture.Data.Array!, texture.Data.Offset + dataOffset, textureData, 0, size);

            if (isCompressedTexture)
            {
                GL.CompressedTexImage2D(target, i, (InternalFormat)info.InternalFormat, width, height, 0, size,
                    textureData);
            }
            else
            {
                GL.TexImage2D(target, i, info.InternalFormat, width, height, 0, info.Format, info.Type,
                    textureData);
            }

            dataOffset += size;

            width >>>= 1;
            height >>>= 1;

            if (width == 0 && height == 0) break;
            if (width == 0) width = 1;
            if (height == 0) height = 1;
        }

        if (!hasMipMaps) GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
}