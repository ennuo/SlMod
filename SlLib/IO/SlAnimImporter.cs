using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using SharpGLTF.Schema2;
using SlLib.Extensions;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Utilities;

namespace SlLib.IO;

public class SlAnimImporter
{
    private enum ChannelAnimType
    {
        None,
        Constant,
        Animated
    }
    
    public static SlAnim Import(ModelRoot gltf, Animation glAnimation, SlSkeleton skeleton)
    {
        const float frameRate = 24.0f;
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

        SlAnim.SlAnimBlendLeaf mainLeaf = anim.BlendBranches[0].Leaf;
        SlAnim.SlAnimBlendLeaf endLeaf = anim.BlendBranches[1].Leaf;
        
        List<AnimationChannel> translationChannels = [];
        List<AnimationChannel> rotationChannels = [];
        List<AnimationChannel> scaleChannels = [];
        
        
        // Gather only channels that are animated
        // TODO: Add check for whether something is just a constant

        int numAnimatedChannels = 0;
        HashSet<Node> nodeSet = [];
        foreach (AnimationChannel? channel in glAnimation.Channels)
        {
            if (!IsChannelAnimated(channel, skeleton)) continue;
            if (!channel.TargetNode.Name.Contains("Neck") && !channel.TargetNode.Name.Contains("Chest")) continue;
            
            nodeSet.Add(channel.TargetNode);
            switch (channel.TargetNodePath)
            {
                case PropertyPath.translation:
                    numAnimatedChannels++;
                    translationChannels.Add(channel);
                    break;
                case PropertyPath.rotation:
                    numAnimatedChannels++;
                    rotationChannels.Add(channel);
                    break;
                case PropertyPath.scale:
                    numAnimatedChannels++;
                    scaleChannels.Add(channel);
                    break;
            }
        }
        
        translationChannels.Sort(SortBySkeletalIndex);
        rotationChannels.Sort(SortBySkeletalIndex);
        scaleChannels.Sort(SortBySkeletalIndex);

        using var leafDataStream = new MemoryStream();
        using var endLeafDataStream = new MemoryStream();
        
        int frameMaskDataOffset = 0;
        byte[] sharedFrameMaskData = new byte[numAnimatedChannels * ((mainLeaf.NumFrames + 7) / 8)];
        
        // Not sure whatever these flags are used for, seems safe to leave them at 0
        byte[] sharedBoneMaskData = new byte[(((rotationChannels.Count + 7) / 8) + 
                                             ((translationChannels.Count + 7) / 8) + 
                                             ((scaleChannels.Count + 7) / 8))];
        
        SetupFrameData(PropertyPath.rotation, rotationChannels, anim.RotationJoints, anim.RotationFrameCommands);
        SetupFrameData(PropertyPath.translation, translationChannels, anim.PositionJoints, anim.PositionFrameCommands);
        SetupFrameData(PropertyPath.scale, scaleChannels, anim.ScaleJoints, anim.ScaleFrameCommands);

        mainLeaf.Offsets[8] = (short)leafDataStream.Position;
        leafDataStream.Write(sharedFrameMaskData);
        mainLeaf.Offsets[13] = (short)leafDataStream.Position;
        leafDataStream.Write(sharedBoneMaskData);
        
        // byte[] empty = new byte[128];
        // for (int i = 0; i < 4; ++i)
        // {
        //     mainLeaf.Offsets[9 + i] = (short)leafDataStream.Position;
        //     leafDataStream.Write(empty);   
        // }
        
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
            
            int byteStride, bitStride;
            switch (path)
            {
                case PropertyPath.rotation:
                {
                    byteStride = 16;
                    bitStride = 128;
                    break;
                }
                case PropertyPath.scale:
                case PropertyPath.translation:
                {
                    byteStride = 12;
                    bitStride = 96;
                    break;
                }
                case PropertyPath.weights:
                {
                    byteStride = 4;
                    bitStride = 32;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(path), path, null);
            }
            
            int endLeafDataSize = 0;
            int endBasisDataOffset = 0;
            endLeafDataSize = SlUtil.Align(channels.Count * byteStride, 0x10);
            
            // Precompute needed data sizes
            int dataSize = 0;
            int basisDataOffset = 0;
            dataSize = SlUtil.Align(channels.Count * byteStride, 0x10);
            int frameDataOffset = dataSize;
            dataSize += path switch
            {
                PropertyPath.rotation => channels.Sum(channel =>
                    channel.GetRotationSampler().GetLinearKeys().Count() * byteStride),
                PropertyPath.scale => channels.Sum(channel =>
                    channel.GetScaleSampler().GetLinearKeys().Count() * byteStride),
                PropertyPath.translation => channels.Sum(channel =>
                    channel.GetTranslationSampler().GetLinearKeys().Count() * byteStride),
                _ => throw new ArgumentOutOfRangeException(nameof(path), path, null)
            };
            dataSize = SlUtil.Align(dataSize, 0x10);
            
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
            
            int frameBitOffset = 0;
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
                        
                        //Console.WriteLine($"{frame}/{mainLeaf.NumFrames}");
                        if (frame >= mainLeaf.NumFrames || frame < 0)
                            throw new ArgumentOutOfRangeException();
                        
                        int bit = (frameMaskDataOffset * 8) + frameBitOffset + frame;
                        sharedFrameMaskData[bit >>> 3] |= (byte)(1 << (7 - (bit & 7)));
                        data.WriteFloat4(value, frameDataOffset);
                        frameDataOffset += byteStride;
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
                        
                        if (frame >= mainLeaf.NumFrames || frame < 0)
                            throw new ArgumentOutOfRangeException();

                        int bit = (frameMaskDataOffset * 8) + frameBitOffset + frame;
                        sharedFrameMaskData[bit >>> 3] |= (byte)(1 << (7 - (bit & 7)));
                        data.WriteFloat3(value, frameDataOffset);
                        frameDataOffset += byteStride;
                    }
                }
                
                basisDataOffset += byteStride;
                endBasisDataOffset += byteStride;
                frameBitOffset += mainLeaf.NumFrames;
            }

            frameMaskDataOffset += ((mainLeaf.NumFrames + 7) / 8) * channels.Count;
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

    public static bool IsChannelAnimated(AnimationChannel channel, SlSkeleton skeleton)
    {
        Node target = channel.TargetNode;
        int index = skeleton.Joints.FindIndex(j => j.Name == target.Name);
        
        // Should this throw an exception?
        if (index == -1)
        {
            Console.WriteLine($"Couldn't find {channel.TargetNode.Name}");
            return false;
        }

        return true;
        
        switch (channel.TargetNodePath)
        {
            case PropertyPath.translation:
            {
                var keys = channel.GetTranslationSampler().GetLinearKeys().ToList();
                bool animated = keys.Any(key => MathF.Abs(Vector3.Distance(key.Value, keys[0].Value)) > 0.01f);
                return animated;
            }
            case PropertyPath.rotation:
            {
                var keys = channel.GetRotationSampler().GetLinearKeys().ToList();
                return keys.Any(key => Math.Abs(Quaternion.Dot(key.Value, keys[0].Value) - 1.0f) > 0.01f);
            }
            case PropertyPath.scale:
            {
                var keys = channel.GetScaleSampler().GetLinearKeys().ToList();
                return keys.Any(key => key.Value != keys.First().Value);
            }
        }
        
        return false;
    }
    
    public static void Retarget(SlAnim anim, SlSkeleton skeleton)
    {
        
    }
}