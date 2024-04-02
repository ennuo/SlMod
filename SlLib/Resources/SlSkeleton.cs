using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Resources.Skeleton;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlSkeleton : ISumoResource
{
    /// <summary>
    ///     Entity attributes in this skeleton.
    /// </summary>
    public List<SlAttribute> Attributes = [];

    /// <summary>
    ///     The joints used in this skeleton.
    /// </summary>
    public List<SlJoint> Joints = [];

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        int poseData = context.ReadInt32(offset + 0xc);
        int jointNameData = context.ReadInt32(offset + 0x10);
        int jointHashData = context.ReadInt32(offset + 0x14);
        int entityNameData = context.ReadInt32(offset + 0x18);
        int attributeNameData = context.ReadInt32(offset + 0x1c);
        int bindPoseMatrixData = context.ReadInt32(offset + 0x20);
        int attributeUserValueData = context.ReadInt32(offset + 0x24);

        int jointCount = context.ReadInt16(poseData + 0x8);
        int attributeCount = context.ReadInt16(poseData + 0xa);

        int jointParentLookupData = poseData + 0x14;
        jointParentLookupData += context.ReadInt32(jointParentLookupData);

        int jointTreeData = poseData + 0x18;
        jointTreeData += context.ReadInt32(jointTreeData);

        int jointTransformData = poseData + 0x1c;
        jointTransformData += context.ReadInt32(jointTransformData);

        int attributeValueData = poseData + 0x20;
        attributeValueData += context.ReadInt32(attributeValueData);

        string[] jointNames = ReadStringTable(jointNameData, jointCount);
        string[] entityNames = ReadStringTable(entityNameData, attributeCount);
        string[] attributeNames = ReadStringTable(attributeNameData, attributeCount);

        for (int i = 0; i < attributeCount; ++i)
            Attributes.Add(new SlAttribute
            {
                Entity = entityNames[i],
                Name = attributeNames[i],
                Value = context.ReadFloat(attributeUserValueData + i * 4),
                Default = context.ReadFloat(attributeValueData + i * 4)
            });

        // Fetch the parents for each attribute
        for (int i = 0; i < attributeCount; ++i)
        {
            int pair = jointTreeData + jointCount * 4 + i * 4;
            int attribute = context.ReadInt16(pair);
            int parent = context.ReadInt16(pair + 2);
            Attributes[attribute].Parent = parent;
        }

        for (int i = 0; i < jointCount; ++i)
        {
            int jointTransform = jointTransformData + i * 0x30;
            int jointBindPose = bindPoseMatrixData + i * 0x40;

            var joint = new SlJoint
            {
                Name = jointNames[i],
                Parent = context.ReadInt16(jointParentLookupData + i * 2),
                Rotation = new Quaternion(context.ReadFloat3(jointTransform), context.ReadFloat(jointTransform + 0xc)),
                Translation = context.ReadFloat3(jointTransform + 0x10),
                Scale = context.ReadFloat3(jointTransform + 0x20),
                BindPose = context.ReadMatrix(jointBindPose)
            };

            Matrix4x4.Invert(joint.BindPose, out Matrix4x4 inverseBindPose);
            joint.InverseBindPose = inverseBindPose;

            Joints.Add(joint);
        }

        return;

        string[] ReadStringTable(int table, int count)
        {
            string[] strings = new string[count];
            for (int i = 0; i < count; ++i)
                strings[i] = context.ReadStringPointer(table + i * 4);
            return strings;
        }
    }
}