using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public abstract class SeInstanceTimeLineEventNodeBase : SeInstanceNode
{
    public bool IsActive;
    public float Start;
    public float Duration;
    public float End;

    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        IsActive = context.ReadBoolean(0x80, wide: true);
        Start = context.ReadFloat(0x84);
        Duration = context.ReadFloat(0x88);
        End = context.ReadFloat(0x8c);
        
        return offset + 0x10;
    }
}