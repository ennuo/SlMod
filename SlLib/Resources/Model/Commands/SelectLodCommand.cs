using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

public class SelectLodCommand : IRenderCommand
{
    public int Type => 0x05;
    public int Size => 0xc + (Thresholds.Count * 0xc);
    
    public short SegmentIndex;
    public short CullSphereIndex;
    public short JointIndex;
    public List<LodThreshold> Thresholds = [];
    
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        SegmentIndex = context.ReadInt16(offset + 4);
        CullSphereIndex = context.ReadInt16(offset + 8);
        JointIndex = context.ReadInt16(offset + 10);
        
        int numThresholds = context.ReadInt16(offset + 6);
        for (int i = 0; i < numThresholds; ++i)
        {
            int address = (offset + 12) + (i * 0xc);
            Thresholds.Add(new LodThreshold
            {
                Threshold = context.ReadFloat2(address),
                LodIndex = context.ReadInt32(address + 8)
            });
        }
    }
    
    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, SegmentIndex, 4);
        context.WriteInt16(commandBuffer, (short)Thresholds.Count, 6);
        context.WriteInt16(commandBuffer, CullSphereIndex, 8);
        context.WriteInt32(commandBuffer, JointIndex, 10);
        for (int i = 0; i < Thresholds.Count; ++i)
        {
            LodThreshold threshold = Thresholds[i];
            int address = 12 + (i * 0xc);
            context.WriteFloat2(commandBuffer, threshold.Threshold, address);
            context.WriteInt32(commandBuffer, threshold.LodIndex, address + 8);
        }
    }
    
    public class LodThreshold
    {
        public Vector2 Threshold;
        public int LodIndex;
    }
}