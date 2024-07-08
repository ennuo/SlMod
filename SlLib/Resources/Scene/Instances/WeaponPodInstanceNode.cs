using System.Numerics;
using SlLib.Enums;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class WeaponPodInstanceNode : SeInstanceTransformNode, IResourceSerializable
{
    public Vector3 PodColor;
    public WeaponPodMessage Message;
    public int AllocationCount;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        PodColor = context.ReadFloat3(0x160);
        Message = (WeaponPodMessage)context.ReadInt32(0x170);
        AllocationCount = context.ReadInt32(0x174);
    }
}