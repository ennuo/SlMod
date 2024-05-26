using System.Runtime.Serialization;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Sprites;

public sealed class SpriteSheet : IDisposable
{
    /// <summary>
    ///     Max dimensions allowed on either axis for sprite sheets.
    /// </summary>
    private const int MaxDimensions = 2048;

    /// <summary>
    ///     The texture data that backs this sprite sheet.
    /// </summary>
    public ArraySegment<byte> Data { get; private set; }

    /// <summary>
    ///     Sprites contained in this sprite sheet.
    /// </summary>
    public readonly List<Sprite> Sprites = [];

    /// <summary>
    ///     The width of this sprite sheet.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    ///     The height of this sprite sheet.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    ///     The internal DDS format of the sprite sheet.
    /// </summary>
    public DXGI_FORMAT Format = DXGI_FORMAT.BC2_UNORM;

    /// <summary>
    ///     Whether or not the sprite sheet has had any modifications made to it.
    ///     <remarks>
    ///         This is for keeping track of if the sprite sheet should be re-saved.
    ///         Don't want to recompress the texture every time an edit is made.
    ///     </remarks>
    /// </summary>
    public bool HasChanges { get; private set; }

    /// <summary>
    ///     Available spaces in the sprite sheet.
    /// </summary>
    private readonly List<Space> _spaces = [];

    /// <summary>
    ///     Image instance
    /// </summary>
    private Image<Rgba32>? _image;

    /// <summary>
    ///     Constructs an empty sprite sheet.
    /// </summary>
    /// <param name="minWidth">Minimum width of the sprite sheet, gets adjusted to nearest power of 2</param>
    /// <param name="minHeight">Minimum height of the sprite sheet, gets adjusted to nearest power of 2</param>
    /// <exception cref="ArgumentException">Thrown if either dimension exceeds maximum dimension</exception>
    public SpriteSheet(int minWidth, int minHeight)
    {
        if (minWidth is <= 0 or > MaxDimensions)
            throw new ArgumentException("Max dimensions for width is " + MaxDimensions);
        if (minHeight is <= 0 or > MaxDimensions)
            throw new ArgumentException("Max dimensions for height is " + MaxDimensions);

        // Dimensions get adjusted to nearest power of 2
        Width = SlUtil.UpperPower(minWidth);
        Height = SlUtil.UpperPower(minHeight);

        _spaces.Add(new Space(0, 0, Width, Height));
        _image = new Image<Rgba32>(Width, Height);
    }

    public SpriteSheet(ArraySegment<byte> data)
    {
        Data = data;

        // Don't want to parse the sprite sheet into an image if
        // we don't need it just yet, so only read the header.
        TexMetadata metadata = DdsUtil.GetTextureInformation(data);

        Width = metadata.Width;
        Height = metadata.Height;
        Format = metadata.Format;
    }

    /// <summary>
    ///     Gets an image instance of this sprite sheet.
    /// </summary>
    /// <returns>RGBA32 image</returns>
    public Image<Rgba32> GetImage()
    {
        EnsureSpriteSheetLoaded();
        return _image!;
    }

    /// <summary>
    ///     Calculates empty spaces from current sprite data.
    /// </summary>
    public void CalculateEmptySpaces()
    {
        // Make sure we're not keeping any old spaces
        _spaces.Clear();

        int nextFreeSpaceY = Sprites.Max(sprite => sprite.Y + sprite.Height);

        // TODO: Find empty spaces in rows

        // Add empty space at the max Y coordinate
        if (nextFreeSpaceY < Height)
            _spaces.Add(new Space(0, nextFreeSpaceY, Width, Height - nextFreeSpaceY));
    }

    /// <summary>
    ///     Caches sprite sheet data to image instance.
    /// </summary>
    private void EnsureSpriteSheetLoaded()
    {
        if (_image != null) return;
        bool success = DdsUtil.ToImage(Data, out _image);
        if (!success)
            throw new SerializationException("Failed to load sprite sheet image data!");
    }

    /// <summary>
    ///     Sets a sprite to an image with the same dimensions.
    /// </summary>
    /// <param name="entry">Sprite entry to set</param>
    /// <param name="spriteImage">Image to set sprite to</param>
    public void SetSpriteImage(Sprite entry, Image<Rgba32> spriteImage)
    {
        int x = entry.X, y = entry.Y;
        int w = entry.Width, h = entry.Height;

        if (w > spriteImage.Width || h > spriteImage.Height)
            throw new ArgumentException("Image dimensions don't fit in sprite dimensions!");

        entry.Width = spriteImage.Width;
        entry.Height = spriteImage.Height;
        
        var rect = new Rectangle(x, y, w, h);
        var pos = new Point(x, y);
        GetImage().Mutate(context =>
        {
            context.Clear(Color.Transparent, rect);
            context.DrawImage(spriteImage, pos, 1.0f);
        });
        

        // Don't recompress the image right now
        HasChanges = true;
    }

    /// <summary>
    ///     Removes a sprite from the sprite sheet.
    /// </summary>
    /// <param name="hash">Hash of sprite to remove</param>
    public void RemoveSprite(int hash)
    {
        Sprite? sprite = Sprites.Find(sprite => sprite.Hash == hash);
        if (sprite == null) return;

        Sprites.Remove(sprite);

        // Add an empty space, so another sprite can replace this later
        _spaces.Add(new Space(sprite.X, sprite.Y, sprite.Width, sprite.Height));
    }

    /// <summary>
    ///     Adds a new sprite to the sprite sheet, replaces the sprite if it already exists.
    /// </summary>
    /// <param name="hash">Unique identifier for the sprite</param>
    /// <param name="image">Image backing the sprite</param>
    /// <returns>Whether or not the sprite was successfully added</returns>
    public bool AddSprite(int hash, Image<Rgba32> image)
    {
        Sprite? sprite = Sprites.Find(s => s.Hash == hash);
        if (sprite != null)
        {
            // If the image can fit in the slot, replace it
            if (image.Width <= sprite.Width && image.Height <= sprite.Height)
            {
                SetSpriteImage(sprite, image);
                return true;
            }
        }
        // Sprite doesn't exist, add a new one
        else
        {
            sprite = new Sprite(this, 0, 0, image.Width, image.Height, hash);
        }

        // If we're trying to add a sprite that doesn't fit, try re-sizing the sheet
        if (!CanFit(image.Width, image.Height))
        {
            if (!CanResize() || !ResizeSheetAndAddSprite(sprite)) return false;
        }
        // Otherwise, just calculate the position normally
        else if (!CalculateSpritePositionInternal(sprite))
        {
            return false;
        }

        SetSpriteImage(sprite, image);

        // Make sure we're not adding a duplicate sprite
        // e.g. if replacing a sprite
        if (!Sprites.Contains(sprite)) Sprites.Add(sprite);

        return true;
    }

    /// <summary>
    ///     Resizes the sprite sheet to fit and add a new sprite.
    /// </summary>
    /// <param name="newSprite">The new sprite to add</param>
    /// <returns>Whether or not the operation succeeded</returns>
    /// <exception cref="Exception">Thrown if an internal error occurs while calculating sprite sheet size</exception>
    private bool ResizeSheetAndAddSprite(Sprite newSprite)
    {
        // Calculate an appropriate width for the sprite sheet using the area of all sprites.
        int spriteArea = newSprite.Area, maxSpriteWidth = newSprite.Width;
        foreach (Sprite sprite in Sprites)
        {
            spriteArea += sprite.Area;
            maxSpriteWidth = Math.Max(maxSpriteWidth, sprite.Width);
        }

        int width = SlUtil.UpperPower((int)Math.Max(Math.Ceiling(Math.Sqrt(spriteArea / 0.95)), maxSpriteWidth));

        // We can't resize the sheet if we're over max dimensions.
        if (width > MaxDimensions) return false;

        // Make sure to add our new sprite to the collection so it gets sorted
        Sprites.Add(newSprite);

        // Re-calculate the positions of all sprites for the new dimensions,
        // height is unbounded.
        _spaces.Clear();
        _spaces.Add(new Space(0, 0, width, MaxDimensions));

        // Need to store the old sprite boxes in a list, because we don't know the height of
        // the image until after we've added all the sprites.
        var oldSpriteBoxes = new List<Rectangle>(Sprites.Count);

        // Sort all sprites by height for better packing.
        Sprites.Sort((z, a) => a.Height - z.Height);

        int height = newSprite.Height;
        foreach (Sprite sprite in Sprites)
        {
            oldSpriteBoxes.Add(new Rectangle(sprite.X, sprite.Y, sprite.Width, sprite.Height));
            if (!CalculateSpritePositionInternal(sprite))
                throw new Exception("Sprite sheet dimensions was calculated in-properly, unable to add sprite!");

            height = Math.Max(height, sprite.Y + sprite.Height);
        }

        Height = SlUtil.UpperPower(height);
        Width = width;

        // This is probably absolutely dreadful on memory usage,
        // but we need to copy all the image data from the old image
        // into a new image, since everything might get re-ordered when resizing.
        var oldImage = GetImage();

        // Copy all the old sprites into the new image
        var newImage = new Image<Rgba32>(Width, Height);
        newImage.Mutate(context =>
        {
            for (int i = 0; i < Sprites.Count; ++i)
            {
                Sprite sprite = Sprites[i];

                // The new sprite doesn't exist in the old image, skip it
                if (sprite == newSprite) continue;

                var position = new Point(sprite.X, sprite.Y);
                context.DrawImage(oldImage, position, oldSpriteBoxes[i], 1.0f);
            }
        });

        // Since the height of the sheet was unbounded, we need to clamp any
        // excess heights back down to the dimensions of the sprite sheet.
        foreach (Space space in _spaces)
            if (space.Y + space.Height > Height)
                space.Height = Height - space.Y;

        // Finalize changes
        _image?.Dispose();
        _image = newImage;
        HasChanges = true;

        return true;
    }

    /// <summary>
    ///     Finds an available position for a sprite in the sheet and sets the position.
    /// </summary>
    /// <param name="sprite">The sprite to add</param>
    /// <returns>Whether or not a position was found in the sprite sheet</returns>
    private bool CalculateSpritePositionInternal(Sprite sprite)
    {
        // Spaces are from biggest to smallest, so iterate backwards
        // to make sure smaller spaces get used up first.
        for (int i = _spaces.Count - 1; i >= 0; --i)
        {
            Space space = _spaces[i];

            // Check if the sprite can fit in the available space
            if (sprite.Width > space.Width || sprite.Height > space.Height) continue;

            sprite.X = space.X;
            sprite.Y = space.Y;

            // Space is an exact match, remove the space
            if (sprite.Width == space.Width && sprite.Height == space.Height)
            {
                Space last = _spaces.Last();
                _spaces.Remove(last);
                if (i < _spaces.Count) _spaces[i] = last;
            }
            // Space matches box height, move the available space to after the box
            else if (sprite.Height == space.Height)
            {
                space.X += sprite.Width;
                space.Width -= sprite.Width;
            }
            // Space matches box width, move the available space to below the box
            else if (sprite.Width == space.Width)
            {
                space.Y += sprite.Height;
                space.Height -= sprite.Height;
            }
            // Doesn't match either, split the space into two more spaces, below and after
            else
            {
                // New space directly after box
                _spaces.Add(new Space(space.X + sprite.Width, space.Y, space.Width - sprite.Width, sprite.Height));

                // Push the old space below the box
                space.Y += sprite.Height;
                space.Height -= sprite.Height;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Flushes any changes made to the sprite sheet.
    /// </summary>
    public void Flush()
    {
        // No point in doing anything if there's no changes.
        if (!HasChanges || _image == null) return;
        Data = DdsUtil.ToDds(_image, Format, generateMips: false, isNormalTexture: false);
        HasChanges = false;
    }

    /// <summary>
    ///     Checks if a sprite can fit into the sheet.
    /// </summary>
    /// <param name="w">Width in pixels</param>
    /// <param name="h">Height in pixels</param>
    /// <returns>Whether or not the sprite can fit</returns>
    public bool CanFit(int w, int h)
    {
        foreach (Space space in _spaces)
            if (w <= space.Width && h <= space.Height)
                return true;

        return false;
    }

    /// <summary>
    ///     Checks if the sprite sheet can be resized.
    /// </summary>
    /// <returns>Whether or not the sprite sheet can be resized</returns>
    public bool CanResize()
    {
        return Width != MaxDimensions || Height != MaxDimensions;
    }

    public void Dispose()
    {
        _image?.Dispose();
    }

    /// <summary>
    ///     Represents a space in the sprite sheet.
    /// </summary>
    /// <param name="x">The top-left x coordinate in pixels</param>
    /// <param name="y">The top-left y coordinate in pixels</param>
    /// <param name="width">The width in pixels</param>
    /// <param name="height">The height in pixels</param>
    private class Space(int x, int y, int width, int height)
    {
        public int Width = width;
        public int Height = height;
        public int X = x;
        public int Y = y;
    }
}