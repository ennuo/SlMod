using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SlLib.SumoTool.Siff.Sprites;

/// <summary>
///     A sprite entry in a texture pack.
/// </summary>
public class Sprite(SpriteSheet sheet, int x, int y, int width, int height, int hash)
{
    /// <summary>
    ///     The sprite sheet that contains this sprite.
    /// </summary>
    public readonly SpriteSheet Sheet = sheet;

    /// <summary>
    ///     The unique identifier for this sprite.
    /// </summary>
    public readonly int Hash = hash;

    /// <summary>
    ///     The X coordinate of the top left of this sprite in pixels.
    /// </summary>
    public int X = x;

    /// <summary>
    ///     The Y coordinate of the top left of this sprite in pixels.
    /// </summary>
    public int Y = y;

    /// <summary>
    ///     The width of this sprite in pixels.
    /// </summary>
    public int Width = width;

    /// <summary>
    ///     The height of this sprite in pixels.
    /// </summary>
    public int Height = height;

    /// <summary>
    ///     The perimeter of the sprite.
    /// </summary>
    public int Perimeter => Height * 2 + Width + 2;

    /// <summary>
    ///     The area of the sprite.
    /// </summary>
    public int Area => Width * Height;

    /// <summary>
    ///     Gets this sprite's image from the sprite sheet.
    /// </summary>
    /// <returns>Sprite image</returns>
    public Image<Rgba32> GetImage()
    {
        var image = Sheet.GetImage();
        var rect = new Rectangle(X, Y, Width, Height);
        return image.Clone(context => { context.Crop(rect); });
    }
}