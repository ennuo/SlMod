using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Resources.Skeleton;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources;

public class SlSkeleton : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     Entity attributes in this skeleton.
    /// </summary>
    public List<SlAttribute> Attributes = [];

    /// <summary>
    ///     The joints used in this skeleton.
    /// </summary>
    public List<SlJoint> Joints = [];

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        int poseData = context.ReadPointer();
        int jointNameData = context.ReadPointer();
        int jointHashData = context.ReadPointer();
        int entityNameData = context.ReadPointer();
        int attributeNameData = context.ReadPointer();
        int bindPoseMatrixData = context.ReadPointer();
        int attributeUserValueData = context.ReadPointer();

        // That's all the pointers in the header, the rest of the parsing is for
        // the bind pose struct
        context.Position = poseData;

        if (context.ReadInt32() != 0x534B454C /* SKEL */)
            throw new SerializationException("Invalid skeleton resource, magic didn't match!");
        context.ReadInt32(); // Data size

        int jointCount = context.ReadInt16();
        int attributeCount = context.ReadInt16();
        // The joint and attribute count is serialized again, but as int32's instead of int16's,
        // just skip them.
        context.Position += 8;
        
        // These pointers are relative to the start of the struct, so they're
        // using integers regardless of the platform.
        int jointParentLookupData = context.Position + context.ReadInt32();
        int jointTreeData = context.Position + context.ReadInt32();
        int jointTransformData = context.Position + context.ReadInt32();
        int attributeValueData = context.Position + context.ReadInt32();

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
                strings[i] = context.ReadStringPointer(table + i * context.Platform.GetPointerSize());
            return strings;
        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Pre-calculate the size of the skeleton pose buffer,
        // don't want too terribly many allocations here.
        int poseBufferSize = 0x30;
        int jointParents = poseBufferSize;
        poseBufferSize += 0x2 * Joints.Count;
        poseBufferSize = SlUtil.Align(poseBufferSize, 0x10);
        int jointHierarchy = poseBufferSize;
        poseBufferSize += 0x4 * Joints.Count;
        int attributeHierarchy = poseBufferSize;
        poseBufferSize += 0x4 * Attributes.Count;
        poseBufferSize = SlUtil.Align(poseBufferSize, 0x10);
        int jointTransforms = poseBufferSize;
        poseBufferSize += Joints.Count * 0x30;
        int defaultAttributes = poseBufferSize;
        poseBufferSize += Attributes.Count * 0x4;

        ISaveBuffer poseBuffer = context.SaveGenericPointer(buffer, 0xc, poseBufferSize, 0x10);
        ISaveBuffer bindMatrixBuffer = context.SaveGenericPointer(buffer, 0x20, Joints.Count * 0x40, 0x10);
        ISaveBuffer attributeBuffer = context.SaveGenericPointer(buffer, 0x24, Attributes.Count * 0x4);
        ISaveBuffer jointNameBuffer = context.SaveGenericPointer(buffer, 0x10, Joints.Count * 0x4);
        ISaveBuffer jointHashBuffer = context.SaveGenericPointer(buffer, 0x14, Joints.Count * 0x4);
        ISaveBuffer entityNameBuffer = context.SaveGenericPointer(buffer, 0x18, Attributes.Count * 0x4);
        ISaveBuffer attributeNameBuffer = context.SaveGenericPointer(buffer, 0x1c, Attributes.Count * 0x4);

        context.WriteInt32(poseBuffer, 0x534B454C, 0x0); // SKEL
        context.WriteInt32(poseBuffer, poseBufferSize, 0x4);
        context.WriteInt16(poseBuffer, (short)Joints.Count, 0x8);
        context.WriteInt16(poseBuffer, (short)Attributes.Count, 0xa);
        context.WriteInt32(poseBuffer, Joints.Count, 0xc);
        context.WriteInt32(poseBuffer, Attributes.Count, 0x10);

        // These pointers are relative to where they're read
        context.WriteInt32(poseBuffer, jointParents - 0x14, 0x14);
        context.WriteInt32(poseBuffer, jointHierarchy - 0x18, 0x18);
        context.WriteInt32(poseBuffer, jointTransforms - 0x1c, 0x1c);
        context.WriteInt32(poseBuffer, defaultAttributes - 0x20, 0x20);

        for (int i = 0; i < Joints.Count; ++i)
        {
            SlJoint joint = Joints[i];

            // Hash array is just used for a quick lookup for a joint's name hash,
            // instead of having to calculate it every time.
            context.WriteInt32(jointHashBuffer, SlUtil.HashString(joint.Name), i * 0x4);
            context.WriteStringPointer(jointNameBuffer, joint.Name, i * 4);
            context.WriteInt16(poseBuffer, (short)joint.Parent, jointParents + i * 2);
            context.WriteMatrix(bindMatrixBuffer, joint.BindPose, i * 64);

            int transform = jointTransforms + i * 0x30;

            for (int j = 0; j < 4; ++j) // Dumb?
                context.WriteFloat(poseBuffer, joint.Rotation[j], transform + j * 0x4);
            context.WriteFloat3(poseBuffer, joint.Translation, transform + 0x10);
            context.WriteFloat3(poseBuffer, joint.Scale, transform + 0x20);

            // I stored the vectors as Float3's, but they're serialized as Float4's,
            // the last component should always be 1.0
            context.WriteFloat(poseBuffer, 1.0f, transform + 0x1c);
            context.WriteFloat(poseBuffer, 1.0f, transform + 0x2c);
        }

        for (int i = 0; i < Attributes.Count; ++i)
        {
            SlAttribute attribute = Attributes[i];
            context.WriteStringPointer(entityNameBuffer, attribute.Entity, i * 0x4);
            context.WriteFloat(poseBuffer, attribute.Default, defaultAttributes + i * 0x4);
            context.WriteFloat(attributeBuffer, attribute.Value, i * 0x4);
        }

        // Doing the attribute names outside of the previous loop to keep
        // serialization consistent with original files.
        for (int i = 0; i < Attributes.Count; ++i)
            context.WriteStringPointer(attributeNameBuffer, Attributes[i].Name, i * 0x4);

        WriteSortedPairs(Joints.Select((joint, index) => new JointHierarchyCacheItem(index, joint.Parent)).ToList(),
            jointHierarchy);
        WriteSortedPairs(
            Attributes.Select((attribute, index) => new JointHierarchyCacheItem(index, attribute.Parent)).ToList(),
            attributeHierarchy);

        context.SaveObject(buffer, Header, 0x0);
        return;

        // The joint/attribute hierarchy is sorted by level
        void WriteSortedPairs(List<JointHierarchyCacheItem> items, int offset)
        {
            // Cache the parent counts for each pair
            items.ForEach(pair => pair.FetchParentCount(items));
            items.Sort((a, z) => ((a.NumParents - z.NumParents) << 16) + (a.Index - z.Index));
            foreach (JointHierarchyCacheItem item in items)
            {
                context.WriteInt16(poseBuffer, (short)item.Index, offset);
                context.WriteInt16(poseBuffer, (short)item.Parent, offset + 2);

                offset += 4;
            }
        }
    }

    /// <inheritdoc />
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x50 : 0x30;
    }
}