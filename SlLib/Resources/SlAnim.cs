using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlAnim : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        throw new NotImplementedException();
    }
}