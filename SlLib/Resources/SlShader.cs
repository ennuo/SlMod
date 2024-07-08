using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlShader : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        
        //throw new NotImplementedException();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        throw new NotImplementedException();
    }
}