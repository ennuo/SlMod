using System.Runtime.Serialization;
using DirectXTexNet;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderTextureResource : IResourceSerializable
{
    public string Name = string.Empty;
    public ArraySegment<byte> ImageData = ArraySegment<byte>.Empty;
    
    public void Load(ResourceLoadContext context)
    {
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
            
            ImageData = context.LoadBuffer(imageData, imageDataSize, true);
        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteStringPointer(buffer, Name, 0x0);
        context.SaveBufferPointer(buffer, ImageData, 0x8, align: 0x10, gpu: true);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform == SlPlatform.Ps3) return 0x20;
        if (platform == SlPlatform.Xbox360) return 0x3c;
        if (platform == SlPlatform.Win32) return 0x10;
        
        throw new SerializationException($"SuRenderTextureResource serialization is unsupported for {platform.Extension}");
    }
}