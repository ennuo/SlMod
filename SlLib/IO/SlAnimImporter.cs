using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpGLTF.Schema2;
using SlLib.Extensions;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Utilities;

namespace SlLib.IO;

public class SlAnimImporter
{
    public static SlAnim Import(ModelRoot gltf, Animation glAnimation, SlSkeleton skeleton)
    {
        const float frameRate = 60.0f;
        const float frameDelta = 1.0f / frameRate;
        int frameCount = (int)Math.Max(Math.Ceiling(glAnimation.Duration * frameRate), 1.0) + 1;
        
        var anim = new SlAnim
        {
            Skeleton = new SlResPtr<SlSkeleton>(skeleton),
            AnimationTime = glAnimation.Duration,
            FrameRate = frameRate,
            BoneCount = (short)skeleton.Joints.Count,
            AttributeCount = (short)skeleton.Attributes.Count,
            FrameCount = (short)frameCount,
            
            // Type 0 means uncompressed, don't feel like dealing with their packing
            // right now, maybe at some point in the future.
            AttributeType = 0,
            PositionType = 0,
            RotationType = 0,
            ScaleType = 0
        };
        
        // Not entirely sure how multiple blend branches work, but most simple
        // animations just have two branches, one with all the actual frame data,
        // then another "exit" branch? That doesn't contain anything
        anim.BlendBranches.Add(new SlAnim.SlAnimBlendBranch
        {
            NumFrames = frameCount - 2,
            Leaf =
            {
                NumFrames = (short)(frameCount - 2)
            }
        });
        
        anim.BlendBranches.Add(new SlAnim.SlAnimBlendBranch
        {
            FrameOffset = frameCount - 1,
            Leaf =
            {
                FrameOffset = (short)(frameCount - 1)
            }
        });
        
        // multiple branches if there's too many frames
        // is it trying to split the data so each branch is under 8000 bytes? or is that just a coincidence.

        SlAnim.SlAnimBlendLeaf mainLeaf = anim.BlendBranches[0].Leaf;
        SlAnim.SlAnimBlendLeaf endLeaf = anim.BlendBranches[1].Leaf;
        
        List<AnimationChannel> translationChannels = [];
        List<AnimationChannel> rotationChannels = [];
        List<AnimationChannel> scaleChannels = [];

        using var constantTranslationStream = new MemoryStream();
        using var constantRotationStream = new MemoryStream();
        using var constantScaleStream = new MemoryStream();
        
        int numAnimatedChannels = 0;
        foreach (AnimationChannel? channel in glAnimation.Channels)
        {
            ChannelAnimType type = IsChannelAnimated(channel, skeleton);
            if (type == ChannelAnimType.Invalid) continue;
            
            switch (channel.TargetNodePath)
            {
                case PropertyPath.translation:
                    if (type == ChannelAnimType.Constant)
                    {
                        anim.ConstantPositionJoints.Add(
                            (short)skeleton.Joints.FindIndex(j => j.Name == channel.TargetNode.Name));
                        anim.ConstantPositionFrameCommands.Add(GetTargetByteStride(channel.TargetNodePath) * 8);
                        Vector3 key = channel.GetTranslationSampler().GetLinearKeys().First().Value;

                        constantTranslationStream.Write(
                            MemoryMarshal.Cast<Vector3, byte>(MemoryMarshal.CreateSpan(ref key, 1)));
                        break;
                    }
                    
                    numAnimatedChannels++;
                    translationChannels.Add(channel);
                    break;
                case PropertyPath.rotation:
                    if (type == ChannelAnimType.Constant)
                    {
                        anim.ConstantRotationJoints.Add(
                            (short)skeleton.Joints.FindIndex(j => j.Name == channel.TargetNode.Name));
                        anim.ConstantRotationFrameCommands.Add(GetTargetByteStride(channel.TargetNodePath) * 8);
                        Quaternion key = channel.GetRotationSampler().GetLinearKeys().First().Value;

                        constantRotationStream.Write(
                            MemoryMarshal.Cast<Quaternion, byte>(MemoryMarshal.CreateSpan(ref key, 1)));
                        break;
                    }
                    
                    numAnimatedChannels++;
                    rotationChannels.Add(channel);
                    break;
                case PropertyPath.scale:

                    if (type == ChannelAnimType.Constant)
                    {
                        anim.ConstantScaleJoints.Add(
                            (short)skeleton.Joints.FindIndex(j => j.Name == channel.TargetNode.Name));
                        anim.ConstantScaleFrameCommands.Add(GetTargetByteStride(channel.TargetNodePath) * 8);
                        Vector3 key = channel.GetScaleSampler().GetLinearKeys().First().Value;

                        constantScaleStream.Write(
                            MemoryMarshal.Cast<Vector3, byte>(MemoryMarshal.CreateSpan(ref key, 1)));
                        break;
                    }
                    
                    numAnimatedChannels++;
                    scaleChannels.Add(channel);
                    break;
            }
        }
        
        constantTranslationStream.Flush();
        constantRotationStream.Flush();
        constantScaleStream.Flush();
        
        anim.PositionAnimData = constantTranslationStream.ToArray();
        anim.RotationAnimData = constantRotationStream.ToArray();
        anim.ScaleAnimData = constantScaleStream.ToArray();
        
        translationChannels.Sort(SortBySkeletalIndex);
        rotationChannels.Sort(SortBySkeletalIndex);
        scaleChannels.Sort(SortBySkeletalIndex);

        using var leafDataStream = new MemoryStream();
        using var endLeafDataStream = new MemoryStream();
        
        int frameMaskDataOffset = 0;
        byte[] sharedFrameMaskData = new byte[numAnimatedChannels * ((mainLeaf.NumFrames + 7) / 8)];
        
        // Not sure whatever these flags are used for, seems safe to leave them at 0
        byte[] basisBoneMaskData = new byte[(((rotationChannels.Count + 7) / 8) + 
                                             (((translationChannels.Count * 3) + 7) / 8) + 
                                             (((scaleChannels.Count * 3) + 7) / 8))];
        
        SetupFrameData(PropertyPath.rotation, rotationChannels, anim.RotationJoints, anim.RotationFrameCommands);
        SetupFrameData(PropertyPath.translation, translationChannels, anim.PositionJoints, anim.PositionFrameCommands);
        SetupFrameData(PropertyPath.scale, scaleChannels, anim.ScaleJoints, anim.ScaleFrameCommands);

        mainLeaf.Offsets[8] = (short)leafDataStream.Position;
        leafDataStream.Write(sharedFrameMaskData);
        mainLeaf.Offsets[13] = (short)leafDataStream.Position;
        leafDataStream.Write(basisBoneMaskData);
        
        byte[] bitMasks3 = new byte[(mainLeaf.NumFrames + 7) / 8];
        byte[] bitMasks1 = new byte[((mainLeaf.NumFrames * 3) + 7) / 8];
        
        if (rotationChannels.Count != 0)
        {
            mainLeaf.Offsets[9] = (short)leafDataStream.Position;
            leafDataStream.Write(bitMasks1);
        }
        
        if (translationChannels.Count != 0)
        {
            mainLeaf.Offsets[10] = (short)leafDataStream.Position;
            leafDataStream.Write(bitMasks3);
        }
        
        if (scaleChannels.Count != 0)
        {
            mainLeaf.Offsets[11] = (short)leafDataStream.Position;
            leafDataStream.Write(bitMasks3);
        }
        
        leafDataStream.Write(new byte[SlUtil.Align(leafDataStream.Position + 0x28, 0x10) - leafDataStream.Position + 0x28]);
        endLeafDataStream.Write(new byte[SlUtil.Align(endLeafDataStream.Position + 0x28, 0x10) - endLeafDataStream.Position + 0x28]);
        
        leafDataStream.Flush();
        endLeafDataStream.Flush();
        mainLeaf.Data = leafDataStream.ToArray();
        endLeaf.Data = endLeafDataStream.ToArray();
        
        anim.BlendBranches[0].Flags = mainLeaf.Data.Count;
        anim.BlendBranches[1].Flags = endLeaf.Data.Count;
        
        
        
        return anim;

        void SetupFrameData(PropertyPath path, List<AnimationChannel> channels, List<short> indices, List<int> sizes)
        {
            if (channels.Count == 0) return;
            
            int byteStride = GetTargetByteStride(path);
            int bitStride = byteStride * 8;
            
            int endLeafDataSize = channels.Count * byteStride;
            int endBasisDataOffset = 0;
            
            // Precompute needed data sizes
            int dataSize = 0;
            int basisDataOffset = 0;
            dataSize = SlUtil.Align(channels.Count * byteStride, 0x10);
            int frameDataOffset = dataSize;
            dataSize += path switch
            {
                PropertyPath.rotation => channels.Sum(channel =>
                    (channel.GetRotationSampler().GetLinearKeys().Count() - 2) * byteStride),
                PropertyPath.scale => channels.Sum(channel =>
                    (channel.GetScaleSampler().GetLinearKeys().Count() - 2) * byteStride),
                PropertyPath.translation => channels.Sum(channel =>
                    (channel.GetTranslationSampler().GetLinearKeys().Count() - 2) * byteStride),
                _ => throw new ArgumentOutOfRangeException(nameof(path), path, null)
            };
            // dataSize = SlUtil.Align(dataSize, 0x10);
            
            byte[] data = new byte[dataSize];
            byte[] endData = new byte[endLeafDataSize];
            
            switch (path)
            {
                case PropertyPath.rotation:
                {
                    mainLeaf.Offsets[0] = (short)(leafDataStream.Position + basisDataOffset);
                    mainLeaf.Offsets[4] = (short)(leafDataStream.Position + frameDataOffset);

                    endLeaf.Offsets[0] = (short)(endLeafDataStream.Position + endBasisDataOffset);
                    break;
                }
                case PropertyPath.translation:
                {
                    mainLeaf.Offsets[1] = (short)(leafDataStream.Position + basisDataOffset);
                    mainLeaf.Offsets[5] = (short)(leafDataStream.Position + frameDataOffset);
                    
                    endLeaf.Offsets[1] = (short)(endLeafDataStream.Position + endBasisDataOffset);
                    break;
                } 
                case PropertyPath.scale:
                {
                    mainLeaf.Offsets[2] = (short)(leafDataStream.Position + basisDataOffset);
                    mainLeaf.Offsets[6] = (short)(leafDataStream.Position + frameDataOffset);
                    
                    endLeaf.Offsets[2] = (short)(endLeafDataStream.Position + endBasisDataOffset);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(path), path, null);
            }
            
            int channelIndex = 0;
            foreach (AnimationChannel channel in channels)
            {
                indices.Add((short)skeleton.Joints.FindIndex(j => j.Name == channel.TargetNode.Name));
                sizes.Add(bitStride);

                IAnimationSampler<Quaternion>? rotSamplerData = null;
                IAnimationSampler<Vector3>? samplerData = null;

                switch (path)
                {
                    case PropertyPath.rotation:
                        rotSamplerData = channel.GetRotationSampler();
                        break;
                    case PropertyPath.scale:
                        samplerData = channel.GetScaleSampler();
                        break;
                    case PropertyPath.translation:
                        samplerData = channel.GetTranslationSampler();
                        break;
                }
                
                if (rotSamplerData != null)
                {
                    var keys = rotSamplerData.GetLinearKeys().ToList();
                    for (int i = 0; i < keys.Count; ++i)
                    {
                        (float Key, Quaternion Value) sample = keys[i];
                        int frame = (int)Math.Ceiling(sample.Key * frameRate);
                        Quaternion quat = sample.Value;
                        Vector4 value = Unsafe.As<Quaternion, Vector4>(ref quat);
                        
                        // Putting the first frame into the basis
                        if (i == 0)
                        {
                            data.WriteFloat4(value, basisDataOffset);
                            continue;
                        }

                        if (i == keys.Count - 1)
                        {
                            endData.WriteFloat4(value, endBasisDataOffset);
                            continue;
                        }
                        
                        // Subtract a frame to account for the basis offset
                        frame--;
                        //frame = i - 1;
                        
                        // Console.WriteLine($"{frame}/{mainLeaf.NumFrames}");
                        
                        if (frame >= mainLeaf.NumFrames || frame < 0)
                            throw new ArgumentOutOfRangeException();
                        
                        int bit = (frameMaskDataOffset * 8) + frame;
                        sharedFrameMaskData[bit >>> 3] |= (byte)(1 << (7 - (bit & 7)));
                        
                        data.WriteFloat4(value, frameDataOffset);
                        frameDataOffset += byteStride;
                        
                        // data.WriteFloat4(value, frameDataOffset + (channelIndex * byteStride * mainLeaf.NumFrames) + (frame * byteStride));
                        //data.WriteFloat4(value, frameDataOffset + (frame * keyframeSize) + (channelIndex * byteStride));
                    }
                }
                else if (samplerData != null)
                {
                    var keys = samplerData.GetLinearKeys().ToList();
                    for (int i = 0; i < keys.Count; ++i)
                    {
                        (float Key, Vector3 Value) sample = keys[i];
                        int frame = (int)Math.Ceiling(sample.Key * frameRate);
                        Vector3 value = sample.Value;
                        
                        // Putting the first frame into the basis
                        if (i == 0)
                        {
                            data.WriteFloat3(value, basisDataOffset);
                            continue;
                        }

                        if (i == keys.Count - 1)
                        {
                            endData.WriteFloat3(value, endBasisDataOffset);
                            continue;
                        }
                        
                        // Subtract a frame to account for the basis offset
                        frame--;
                        //frame = i - 1;
                        
                        if (frame >= mainLeaf.NumFrames || frame < 0)
                            throw new ArgumentOutOfRangeException();

                        int bit = (frameMaskDataOffset * 8) + frame;
                        sharedFrameMaskData[bit >>> 3] |= (byte)(1 << (7 - (bit & 7)));
                        
                        data.WriteFloat3(value, frameDataOffset);
                        frameDataOffset += byteStride;
                        
                        //data.WriteFloat3(value, frameDataOffset + (frame * keyframeSize) + (channelIndex * byteStride));
                        //data.WriteFloat3(value, frameDataOffset + (channelIndex * byteStride * mainLeaf.NumFrames) + (frame * byteStride));
                    }
                }
                
                basisDataOffset += byteStride;
                endBasisDataOffset += byteStride;
                frameMaskDataOffset += ((mainLeaf.NumFrames + 7) / 8);
                channelIndex++;
            }
            
            leafDataStream.Write(data);
            endLeafDataStream.Write(endData);
        }

        int SortBySkeletalIndex(AnimationChannel a, AnimationChannel z)
        {
            int indexA = skeleton.Joints.FindIndex(j => j.Name == a.TargetNode.Name);
            int indexB = skeleton.Joints.FindIndex(j => j.Name == z.TargetNode.Name);

            return indexA - indexB;
        }
    }

    public static ChannelAnimType IsChannelAnimated(AnimationChannel channel, SlSkeleton skeleton)
    {
        Node target = channel.TargetNode;
        int index = skeleton.Joints.FindIndex(j => j.Name == target.Name);
        
        // Should this throw an exception?
        if (index == -1) return ChannelAnimType.Invalid;
        
        switch (channel.TargetNodePath)
        {
            case PropertyPath.translation:
            {
                var keys = channel.GetTranslationSampler().GetLinearKeys().ToList();
                if (keys.Count == 2) return ChannelAnimType.Constant;
                
                bool animated = keys.Any(key => MathF.Abs(Vector3.Distance(key.Value, keys[0].Value)) > 0.01f);
                return animated ? ChannelAnimType.Animated : ChannelAnimType.Constant;
            }
            case PropertyPath.rotation:
            {
                var keys = channel.GetRotationSampler().GetLinearKeys().ToList();
                if (keys.Count == 2) return ChannelAnimType.Constant;
                
                bool animated = keys.Any(key => Math.Abs(Quaternion.Dot(key.Value, keys[0].Value) - 1.0f) > 0.01f);
                return animated ? ChannelAnimType.Animated : ChannelAnimType.Constant;
            }
            case PropertyPath.scale:
            {
                var keys = channel.GetScaleSampler().GetLinearKeys().ToList();
                if (keys.Count == 2) return ChannelAnimType.Constant;
                
                bool animated = keys.Any(key => key.Value != keys.First().Value);
                return animated ? ChannelAnimType.Animated : ChannelAnimType.Constant;
            }
        }
        
        return ChannelAnimType.Invalid;
    }
    
    public static void Retarget(SlAnim anim, SlSkeleton skeleton)
    {
        
    }

    public static int GetTargetByteStride(PropertyPath path)
    {
        return path switch
        {
            PropertyPath.rotation => 0x10,
            PropertyPath.weights => 0x4,
            _ => 0xc
        };
    }
    
    public enum ChannelAnimType
    {
        Invalid,
        Constant,
        Animated
    }
}