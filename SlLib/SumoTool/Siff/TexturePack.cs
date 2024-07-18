using System.Numerics;
using System.Runtime.Serialization;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest.GCM;
using SlLib.SumoTool.Siff.Sprites;
using SlLib.Utilities;
using Image = SixLabors.ImageSharp.Image;

namespace SlLib.SumoTool.Siff;

public class TexturePack : ISumoToolResource
{
    public SiffResourceType Type => SiffResourceType.TexturePack;

    /// <summary>
    ///     Sprite sheets stored in this texture pack.
    /// </summary>
    public readonly List<SpriteSheet> Sheets = [];

    /// <summary>
    ///     Gets the number of sprites in this texture pack.
    /// </summary>
    /// <returns>Number of sprites</returns>
    public int GetSpriteCount()
    {
        return Sheets.Sum(sheet => sheet.Sprites.Count);
    }

    /// <summary>
    ///     Gets all sprite instances from sprite sheets.
    /// </summary>
    /// <returns>All sprites in the sprite sheets</returns>
    public List<Sprite> GetSprites()
    {
        var sprites = new List<Sprite>(GetSpriteCount());
        foreach (SpriteSheet sheet in Sheets)
            sprites.AddRange(sheet.Sprites);
        return sprites;
    }

    /// <summary>
    ///     Checks if a sprite with a given hash exists.
    /// </summary>
    /// <param name="hash">Sprite name hash to find</param>
    /// <returns>Whether the sprite exists</returns>
    public bool HasSprite(int hash)
    {
        return Sheets.Any(sheet => sheet.Sprites.Exists(sprite => sprite.Hash == hash));
    }

    /// <summary>
    ///     Gets a sprite by its hash.
    /// </summary>
    /// <param name="hash">Sprite name hash to find</param>
    /// <returns>Sprite, if found in any sprite sheet</returns>
    public Sprite? GetSprite(int hash)
    {
        foreach (SpriteSheet sheet in Sheets)
        {
            Sprite? sprite = sheet.Sprites.Find(sprite => sprite.Hash == hash);
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    /// <summary>
    ///     Adds a sprite to the texture pack from a file on disk, replaces one if it already exists.
    /// </summary>
    /// <param name="hash">Unique identifier for sprite to add</param>
    /// <param name="path">Path to image file</param>
    /// <exception cref="FileNotFoundException">Thrown if file doesn't exist</exception>
    public void AddSprite(int hash, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{path} doesn't exist!");

        using var image = Image.Load<Rgba32>(path);
        AddSprite(hash, image);
    }

    /// <summary>
    ///     Adds a sprite to the texture pack, replaces one if it already exists.
    /// </summary>
    /// <param name="hash">Unique identifier for sprite to add</param>
    /// <param name="image">Sprite image</param>
    public void AddSprite(int hash, Image<Rgba32> image)
    {
        SpriteSheet sheet;
        Sprite? sprite = GetSprite(hash);

        // Try replacing the sprite in the sheet it's from
        if (sprite != null)
        {
            sheet = sprite.Sheet;
            if (sheet.AddSprite(hash, image)) return;

            // If we can't replace it, remove it from the sheet,
            // and try putting it somewhere else
            sheet.RemoveSprite(hash);
        }

        // The last sprite sheet in the last is generally less populated than the first,
        // so start inserting backwards
        for (int i = Sheets.Count - 1; i >= 0; --i)
        {
            sheet = Sheets[i];

            // TEMP: Too lazy to deal with uncompressed textures right now
            if (!TexHelper.Instance.IsCompressed(sheet.Format)) continue;

            if (sheet.AddSprite(hash, image)) return;
        }

        // If it was unable to fit anywhere, add a new sprite sheet
        sheet = new SpriteSheet(image.Width, image.Height);
        sheet.AddSprite(hash, image);
        Sheets.Add(sheet);
    }

    /// <summary>
    ///     mamma da mia
    /// </summary>
    public void DebugRepackAllSpriteSheets()
    {
        var sprites = GetSprites();
        Sheets.Clear();
        sprites.Sort((z, a) => a.Height - z.Height);
        foreach (Sprite sprite in sprites)
            AddSprite(sprite.Hash, sprite.GetImage());
    }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        int numEntries = context.ReadInt32();
        int numTextures = context.ReadInt32();
        int textureList = context.ReadPointer();
        int spriteList = context.Position;
        
        bool isPs3 = context.Platform == SlPlatform.Ps3;
        List<GcmTextureEntry> gcmTextureEntries = [];
        
        context.Position = textureList;
        if (isPs3)
        {
            for (int i = 0; i < numTextures; ++i)
            {
                var entry = context.LoadReference<GcmTextureEntry>();
                gcmTextureEntries.Add(entry);
                Sheets.Add(new SpriteSheet(entry.ImageData));   
            }
        }
        else
        {
            for (int i = 0; i < numTextures; ++i)
            {
                int textureData = context.ReadPointer(out bool isDataFromGpu);
                int textureSize = context.ReadInt32();
                var data = context.LoadBuffer(textureData, textureSize, isDataFromGpu);
                Sheets.Add(new SpriteSheet(data));
            }
        }
        
        context.Position = spriteList;
        for (int i = 0; i < numEntries; ++i)
        {
            int hash = context.ReadInt32();
            Vector2 tl = context.ReadFloat2();
            Vector2 tr = context.ReadFloat2();
            Vector2 br = context.ReadFloat2();
            Vector2 bl = context.ReadFloat2();
            int textureIndex;

            if (isPs3)
            {
                GcmTextureEntry entry = context.LoadPointer<GcmTextureEntry>() ??
                                        throw new SerializationException("Sprite cannot have NULL image data!");
                textureIndex = gcmTextureEntries.IndexOf(entry);

                var scale = new Vector2(entry.Texture.Width, entry.Texture.Height);
                tl /= scale;
                tr /= scale;
                br /= scale;
                bl /= scale;
            }
            else textureIndex = context.ReadInt32();
            
            SpriteSheet sheet = Sheets[textureIndex];
            int width = sheet.Width, height = sheet.Height;

            int x = (int)(tl.X * width);
            int y = (int)(tl.Y * height);
            int w = (int)((br.X - bl.X) * width);
            int h = (int)((br.Y - tr.Y) * height);

            sheet.Sprites.Add(new Sprite(sheet, x, y, w, h, hash));
        }

        foreach (SpriteSheet sheet in Sheets)
            sheet.CalculateEmptySpaces();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        ISaveBuffer textureListBuffer = context.SaveGenericPointer(buffer, 0x8, Sheets.Count * 0x8);
        var sprites = GetSprites();
        context.WriteInt32(buffer, sprites.Count, 0x0);
        context.WriteInt32(buffer, Sheets.Count, 0x4);

        for (int i = 0; i < sprites.Count; ++i)
        {
            Sprite sprite = sprites[i];
            SpriteSheet sheet = sprite.Sheet;
            float w = sheet.Width, h = sheet.Height;

            int address = 0xc + i * 0x28;

            var tl = new Vector2(sprite.X / w, sprite.Y / h);
            var tr = new Vector2((sprite.X + sprite.Width) / w, sprite.Y / h);
            var br = new Vector2((sprite.X + sprite.Width) / w, (sprite.Y + sprite.Height) / h);
            var bl = new Vector2(sprite.X / w, (sprite.Y + sprite.Height) / h);

            int index = Sheets.IndexOf(sheet);
            if (index == -1)
                throw new SerializationException("Sprite doesn't belong to any sprite sheet in this texture pack!");

            context.WriteInt32(buffer, sprite.Hash, address + 0x0);
            context.WriteFloat2(buffer, tl, address + 0x4);
            context.WriteFloat2(buffer, tr, address + 0xc);
            context.WriteFloat2(buffer, br, address + 0x14);
            context.WriteFloat2(buffer, bl, address + 0x1c);
            context.WriteInt32(buffer, index, address + 0x24);
        }

        for (int i = 0; i < Sheets.Count; ++i)
        {
            Sheets[i].Flush(); // Make sure to flush texture pack data

            var texture = Sheets[i].Data;
            int address = i * 0x8;

            context.SaveBufferPointer(textureListBuffer, texture, address);
            context.WriteInt32(textureListBuffer, texture.Count, address + 0x4);
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        int size = 0xc + GetSpriteCount() * 0x28;
        if (platform.Is64Bit) size += 0x4;
        return size;
    }

    private class GcmTextureEntry : IResourceSerializable
    {
        public CellGcmTexture Texture;
        public byte[] ImageData;

        public void Load(ResourceLoadContext context)
        {
            Texture = context.LoadPointer<CellGcmTexture>() ??
                      throw new SerializationException("Gcm texture cannot be NULL!");
            
            if (Texture.Cubemap || !CellGcmTexture.IsDXT(Texture.Format))
                throw new SerializationException("Unsupported texture type in texture pack!");
            
            int imageBufferAddress = context.ReadPointer();
            int imageDataSize = 0;
            
            int w = Texture.Width, h = Texture.Height;
            for (int i = 0; i < Texture.MipCount; ++i)
            { 
                imageDataSize += CellGcmTexture.GetImageSize(Texture.Format, w, h);

                w >>>= 1;
                h >>>= 1;
                
                if (w == 0 && h == 0) break;
                if (w == 0) w = 1;
                if (h == 0) h = 1;
            }

            var imageData = context.LoadBuffer(imageBufferAddress, imageDataSize, true);

            DXGI_FORMAT format = Texture.Format switch
            {
                CellGcmEnumForGtf.DXT1 => DXGI_FORMAT.BC1_UNORM,
                CellGcmEnumForGtf.DXT3 => DXGI_FORMAT.BC2_UNORM,
                CellGcmEnumForGtf.DXT5 => DXGI_FORMAT.BC3_UNORM,
                _ => throw new SerializationException("Unsupported texture type in texture pack!")
            };
            
            ImageData = DdsUtil.CompleteFileHeader(imageData, format, Texture.Width, Texture.Height, Texture.MipCount);
        }
        
        public int GetSizeForSerialization(SlPlatform platform, int version)
        {
            return 0x8;
        }
    }
}