﻿using System.Numerics;

namespace SlLib.Resources.Model;

public class SlModelInstanceData
{
    public Matrix4x4 InstanceWorldMatrix;
    public Matrix4x4 InstanceBindMatrix;
    public int RenderMask;
    public int LodGroupFlags;
    public short Visibility;
    public Matrix4x4 WorldMatrix;
    public Matrix4x4 CullMatrix;
}