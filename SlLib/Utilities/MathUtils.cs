using System.Numerics;

namespace SlLib.Utilities;

public static class MathUtils
{
    public const float Deg2Rad = (float)(Math.PI * 2) / 360;
    public const float Rad2Deg = (float)(360 / (Math.PI * 2));
    
    /// <summary>
    ///     Clamps a value to a range
    /// </summary>
    /// <param name="v">Float value</param>
    /// <param name="min">Minimum float value</param>
    /// <param name="max">Maximum float value</param>
    /// <returns>Clamped value</returns>
    public static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        return v > max ? max : v;
    }
    
    // from http://answers.unity3d.com/questions/467614/what-is-the-source-code-of-quaternionlookrotation.html
    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Cross(forward, right);
        var m00 = right.X;
        var m01 = right.Y;
        var m02 = right.Z;
        var m10 = up.X;
        var m11 = up.Y;
        var m12 = up.Z;
        var m20 = forward.X;
        var m21 = forward.Y;
        var m22 = forward.Z;
        
        float num8 = (m00 + m11) + m22;
        var quaternion = new Quaternion();
        if (num8 > 0f)
        {
            var num = (float)Math.Sqrt(num8 + 1f);
            quaternion.W = num * 0.5f;
            num = 0.5f / num;
            quaternion.X = (m12 - m21) * num;
            quaternion.Y = (m20 - m02) * num;
            quaternion.Z = (m01 - m10) * num;
            return quaternion;
        }
        if ((m00 >= m11) && (m00 >= m22))
        {
            var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
            var num4 = 0.5f / num7;
            quaternion.X = 0.5f * num7;
            quaternion.Y = (m01 + m10) * num4;
            quaternion.Z = (m02 + m20) * num4;
            quaternion.W = (m12 - m21) * num4;
            return quaternion;
        }
        if (m11 > m22)
        {
            var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
            var num3 = 0.5f / num6;
            quaternion.X = (m10 + m01) * num3;
            quaternion.Y = 0.5f * num6;
            quaternion.Z = (m21 + m12) * num3;
            quaternion.W = (m20 - m02) * num3;
            return quaternion;
        }
        var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
        var num2 = 0.5f / num5;
        quaternion.X = (m20 + m02) * num2;
        quaternion.Y = (m21 + m12) * num2;
        quaternion.Z = 0.5f * num5;
        quaternion.W = (m01 - m10) * num2;
        return quaternion;
    }
    
    /// <summary>
    ///     Converts a rotation vector in radians to a quaternion.
    /// </summary>
    /// <param name="v">Rotation in radians</param>
    /// <returns>Quaternion</returns>
    public static Quaternion ToQuaternion(Vector3 v)
    {
        float cy = (float)Math.Cos(v.Z * 0.5);
        float sy = (float)Math.Sin(v.Z * 0.5);
        float cp = (float)Math.Cos(v.Y * 0.5);
        float sp = (float)Math.Sin(v.Y * 0.5);
        float cr = (float)Math.Cos(v.X * 0.5);
        float sr = (float)Math.Sin(v.X * 0.5);

        return new Quaternion
        {
            W = (cr * cp * cy + sr * sp * sy),
            X = (sr * cp * cy - cr * sp * sy),
            Y = (cr * sp * cy + sr * cp * sy),
            Z = (cr * cp * sy - sr * sp * cy)
        };
    }

    /// <summary>
    ///     Converts euler angles in degrees to a quaternion
    /// </summary>
    /// <param name="rotation">Euler angles in degrees</param>
    /// <returns>Quaternion</returns>
    public static Quaternion FromEulerAngles(Vector3 rotation)
    {
        rotation *= Deg2Rad;

        var xRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rotation.X);
        var yRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotation.Y);
        var zRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation.Z);
        
        Quaternion q = (zRotation * yRotation * xRotation);
        
        if (q.W < 0) q *= -1;

        return q;
    }

    public static Vector3 ToEulerAngles(Quaternion q)
    {
        var mat = Matrix4x4.CreateFromQuaternion(q);
        float x, y, z;
        y = (float)Math.Asin(Clamp(mat.M13, -1, 1));

        if (Math.Abs(mat.M13) < 0.99999)
        {
            x = (float)Math.Atan2(-mat.M23, mat.M33);
            z = (float)Math.Atan2(-mat.M12, mat.M11);
        }
        else
        {
            x = (float)Math.Atan2(mat.M32, mat.M22);
            z = 0;
        }

        return new Vector3(x, y, z) * -1 * Rad2Deg;
    }
}