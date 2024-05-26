using System.Text;
using SlLib.Resources.Database;

namespace SlLib.Utilities;

/// <summary>
///     Common utility functions
/// </summary>
public static class SlUtil
{
    /// <summary>
    ///     Computes the FNV hash of a given string.
    /// </summary>
    /// <param name="s">String to hash</param>
    /// <returns>The hash of the string</returns>
    public static int HashString(string s)
    {
        const uint prime = 0x1000193;
        uint hash = 0x811c9dc5;

        for (int i = 0; i < s.Length; ++i)
        {
            hash *= prime;
            hash ^= s[i];
        }

        return (int)(hash >>> 0);
    }

    /// <summary>
    ///     Mixes 3 32-bit values reversibly.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    private static void JenkinsMix(ref uint a, ref uint b, ref uint c)
    {
        a -= b;
        a -= c;
        a ^= c >> 13;
        b -= c;
        b -= a;
        b ^= a << 8;
        c -= a;
        c -= b;
        c ^= b >> 13;
        a -= b;
        a -= c;
        a ^= c >> 12;
        b -= c;
        b -= a;
        b ^= a << 16;
        c -= a;
        c -= b;
        c ^= b >> 5;
        a -= b;
        a -= c;
        a ^= c >> 3;
        b -= c;
        b -= a;
        b ^= a << 10;
        c -= a;
        c -= b;
        c ^= b >> 15;
    }

    /// <summary>
    ///     Computes the Jenkins hash of a given string.
    /// </summary>
    /// <param name="s">String to hash</param>
    /// <returns>The hash of the string</returns>
    public static int SumoHash(string s)
    {
        byte[] k = Encoding.ASCII.GetBytes(s);
        int offset = 0;
        int len = k.Length;

        uint a = 0x9e3779b9;
        uint b = a;
        uint c = 0x4c11db7;

        while (len >= 12)
        {
            a += BitConverter.ToUInt32(k, offset + 0);
            b += BitConverter.ToUInt32(k, offset + 4);
            c += BitConverter.ToUInt32(k, offset + 8);

            JenkinsMix(ref a, ref b, ref c);

            offset += 12;
            len -= 12;
        }

        c += (uint)k.Length;

        switch (len)
        {
            case 11:
                c += (uint)k[offset + 10] << 24;
                goto case 10;
            case 10:
                c += (uint)k[offset + 9] << 16;
                goto case 9;
            case 9:
                c += (uint)k[offset + 8] << 8;
                goto case 8;
            case 8:
                b += (uint)k[offset + 7] << 24;
                goto case 7;
            case 7:
                b += (uint)k[offset + 6] << 16;
                goto case 6;
            case 6:
                b += (uint)k[offset + 5] << 8;
                goto case 5;
            case 5:
                b += k[offset + 4];
                goto case 4;
            case 4:
                a += (uint)k[offset + 3] << 24;
                goto case 3;
            case 3:
                a += (uint)k[offset + 2] << 16;
                goto case 2;
            case 2:
                a += (uint)k[offset + 1] << 8;
                goto case 1;
            case 1:
                a += k[offset + 0];
                break;
        }

        JenkinsMix(ref a, ref b, ref c);

        return (int)c;
    }

    /// <summary>
    ///     Calculates resource type ID from string.
    /// </summary>
    /// <param name="s">Resource type name to hash</param>
    /// <returns>Resource type ID</returns>
    public static SlResourceType ResourceId(string s)
    {
        return (SlResourceType)(HashString(s) >>> 4);
    }

    /// <summary>
    ///     Aligns an offset to a specified boundary.
    /// </summary>
    /// <param name="offset">Data offset</param>
    /// <param name="align">Alignment boundary</param>
    /// <returns>Aligned offset</returns>
    public static int Align(int offset, int align)
    {
        if (offset % align != 0)
            offset += align - offset % align;
        return offset;
    }

    /// <summary>
    ///     Aligns an offset to a specified boundary.
    /// </summary>
    /// <param name="offset">Data offset</param>
    /// <param name="align">Alignment boundary</param>
    /// <returns>Aligned offset</returns>
    public static long Align(long offset, int align)
    {
        if (offset % align != 0)
            offset += align - offset % align;
        return offset;
    }
    
    public static string GetShortName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "NoName";
        int start = name.Length;
        while (start > 0)
        {
            char c = name[start - 1];
            if (c is '|' or '\\' or '/' or ':') break;
            start--;
        }

        return name[start..];
    }

    /// <summary>
    ///     Gets the next power of 2.
    /// </summary>
    /// <param name="v">Value</param>
    /// <returns>Value rounded to the next power of 2</returns>
    public static int UpperPower(int v)
    {
        v--;
        v |= v >>> 1;
        v |= v >>> 2;
        v |= v >>> 4;
        v |= v >>> 8;
        v |= v >>> 16;
        return ++v;
    }
}