using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Database;

public class SlResourceHeader : IResourceSerializable
{
    /// <summary>
    ///     The platform this resource belongs to.
    /// </summary>
    public SlPlatform Platform = SlPlatform.Win32;

    /// <summary>
    ///     The unique identifier associated with this resource
    ///     <remarks>
    ///         This hash is often just the hash of the name.
    ///     </remarks>
    /// </summary>
    public int Id;

    /// <summary>
    ///     The name of this resource.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The reference count for this resource.
    ///     <remarks>
    ///         I don't use this in this library, but it's a field that gets serialized.
    ///     </remarks>
    /// </summary>
    public int Ref = 1;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        // Make sure to update the platform attribute based on our current context
        Platform = context.Platform;

        Id = context.ReadInt32();
        // They switched the order of these fields around the Android version.
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            Ref = context.ReadInt32();
            Name = context.ReadStringPointer();
        }
        else
        {
            Name = context.ReadStringPointer();
            Ref = context.ReadInt32();
        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Id, 0);
        context.WriteStringPointer(buffer, Name, 0x4);
        context.WriteInt32(buffer, Ref, 0x8);
    }

    /// <inheritdoc />
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        // Really only have to account for the name pointer here.
        return platform.Is64Bit ? 0x10 : 0xc;
    }

    public void SetName(string tag)
    {
        Name = tag;
        Id = SlUtil.HashString(tag);
    }
}