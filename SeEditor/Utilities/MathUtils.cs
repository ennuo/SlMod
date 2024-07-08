using System.Numerics;

namespace SeEditor.Utilities;

public class MathUtils
{
    public const float Deg2Rad = (float)(Math.PI * 2) / 360;
    public const float Rad2Deg = (float)(360 / (Math.PI * 2));
    
    public static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        return v > max ? max : v;
    }

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