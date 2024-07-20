using System.Runtime.Serialization;
using DirectXTexNet;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest.DirectX.Xenos;
using SlLib.SumoTool.Siff.Forest.GCM;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderTextureResource : IResourceSerializable
{
    public string Name = string.Empty;
    public ArraySegment<byte> ImageData = ArraySegment<byte>.Empty;
    
    public void Load(ResourceLoadContext context)
    {
        int start = context.Position;
        
        Name = context.ReadStringPointer();
        if (context.Platform == SlPlatform.Win32)
        {
            context.ReadInt32(); // ??? always 0?
            int imageData = context.ReadPointer();
            context.ReadInt32(); // ??? always 0?
            
            // Basically nothing serializes an actual size field in forest files,
            // but at least on Windows it's just a DDS file, so we can query the size.
            var header = context.LoadBuffer(imageData, 0x80, true);
            var info = DdsUtil.GetTextureInformation(header);
            int imageDataSize = 0x80;
            
            int numFaces = info.IsCubemap() ? 6 : 1;
            for (int n = 0; n < numFaces; ++n)
            {
                int w = info.Width, h = info.Height;
                for (int i = 0; i < info.MipLevels; ++i)
                {

                    TexHelper.Instance.ComputePitch(info.Format, w, h, out long rowPitch, out long slicePitch,
                        CP_FLAGS.NONE);

                    imageDataSize += (int)slicePitch;
                
                    w >>>= 1;
                    h >>>= 1;

                    if (w == 0 && h == 0) break;
                    if (w == 0) w = 1;
                    if (h == 0) h = 1;
                }    
            }
            
            // Console.WriteLine($"{Name} {info.Width}x{info.Height} (cube={info.IsCubemap()}) (mips={info.MipLevels}) (format={info.Format})");
            
            ImageData = context.LoadBuffer(imageData, imageDataSize, true);
        }

        if (context.Platform == SlPlatform.Ps3)
        {
            var texture = context.LoadObject<CellGcmTexture>();
            int imageBufferAddress = context.ReadPointer();
            int imageDataSize = 0;

            Console.WriteLine(Name + " : " + texture.Format);

        }

        if (context.Platform == SlPlatform.Xbox360)
        {
            // wawawwa debug swap
            if (true)
            {
                ImageData = ArraySegment<byte>.Empty;

                string local = "C:/Users/Aidan/Desktop/DLC/TEXTURES/" + Name + ".DDS";
                if (File.Exists(local))
                    ImageData = File.ReadAllBytes("C:/Users/Aidan/Desktop/DLC/TEXTURES/" + Name + ".DDS");
                else
                {
                    //Console.WriteLine($"MISSING: {Name}");
                    ImageData = File.ReadAllBytes("F:/sart/white.dds");   
                }
                return;
            }
            
            
            
            
            int realOffset = (context._data.Offset + start);
            
            var type = (TextureFormat)(context.ReadInt8(start + 0x36) >> 2);

            // 2 = k_8
            // 18 = k_DXT1
            // 32 = k_16_16_16_16_FLOAT 
            // 34 = k_32_32
            // 51 = k_DXT1_AS_16_16_16_16 

            int bits = context.ReadInt32(start + 0x28);
            
            // something in 0x30 is the mips im guessing
            
            int height = ((bits >> 13) & 0x3fff) + 1;
            int width = (bits & 0x1fff) + 1;

            var format = DXGI_FORMAT.UNKNOWN;
            switch (type)
            {
                case TextureFormat.k_DXT1_AS_16_16_16_16:
                case TextureFormat.k_DXT1: 
                    format = DXGI_FORMAT.BC1_UNORM;
                    break;
                case TextureFormat.k_DXT3A:
                case TextureFormat.k_DXT3A_AS_1_1_1_1:
                    format = DXGI_FORMAT.BC2_UNORM;
                    break;
                case TextureFormat.k_DXT4_5_AS_16_16_16_16:
                case TextureFormat.k_DXT4_5:
                case TextureFormat.k_DXT5A:
                    format = DXGI_FORMAT.BC3_UNORM;
                    break;
                case TextureFormat.k_8:
                    format = DXGI_FORMAT.R8_UNORM;
                    break;
                // case TextureFormat.k_8_8:
                //     format = DXGI_FORMAT.R8G8_UNORM;
                //     break;
                case TextureFormat.k_8_A:
                    format = DXGI_FORMAT.R8_UNORM;
                    break;
                case TextureFormat.k_5_6_5:
                    format = DXGI_FORMAT.B5G6R5_UNORM;
                    break;
                case TextureFormat.k_32_32:
                    format = DXGI_FORMAT.R32G32_UINT;
                    break;
            }

            if (format == DXGI_FORMAT.UNKNOWN)
            {
                // temp shit woo hoo white texture
                ImageData = File.ReadAllBytes("F:/sart/white.dds");
                return;
            }

            Console.WriteLine(format);
            int imageData = context.ReadInt32(start + 0x3c);
            int imageDataSize = 0x0;
            
            int mips = 0;
            int w = width, h = height;
            while (true)
            {

                TexHelper.Instance.ComputePitch(format, w, h, out long rowPitch, out long slicePitch,
                    CP_FLAGS.NONE);

                imageDataSize += (int)slicePitch;
                mips++;

                break;
                
                w >>>= 1;
                h >>>= 1;

                if (w == 0 && h == 0) break;
                if (w == 0) w = 1;
                if (h == 0) h = 1;
            }


            byte[] dds = DdsUtil.CompleteFileHeader(context.LoadBuffer(imageData, imageDataSize, true), format, width, height, mips);
            ImageData = dds;

        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteStringPointer(buffer, Name, 0x0);
        context.SaveBufferPointer(buffer, ImageData, 0x8, align: 0x80, gpu: true);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform == SlPlatform.Ps3) return 0x20;
        if (platform == SlPlatform.Xbox360) return 0x3c;
        if (platform == SlPlatform.Win32) return 0x10;
        
        throw new SerializationException($"SuRenderTextureResource serialization is unsupported for {platform.Extension}");
    }
}