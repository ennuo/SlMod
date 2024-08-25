using System.Numerics;
using System.Text;
using SlLib.Extensions;
using SlLib.Resources.Database;

namespace SlLib.Utilities;

/// <summary>
///     Common utility functions
/// </summary>
public static class SlUtil
{
    private static readonly int[] MaskTable = new int[33];
    private static readonly int[] ExponentBiasTable = [0x80, 0x7f, 0x7e, 0x7c, 0x78, 0x70, 0x60, 0x40, 0x0, -0x80, -0x180];
    private static readonly int[] SignMaskTable = new int[32];
    private static readonly int[] SignExtendTable = new int[33];
    
    static SlUtil()
    {
        for (int i = 0; i < 32; ++i)
        {
            MaskTable[i] = (1 << i) - 1;
            SignMaskTable[i] = 1 << i;
            SignExtendTable[i] = ~((1 << i) - 1);
        }
        
        MaskTable[32] = -1;
        SignExtendTable[32] = 0;
    }
    
    /// <summary>
    ///     Reverses the bits of a 32-bit integer.
    /// </summary>
    /// <param name="value">Number to reverse</param>
    /// <returns>Reversed bits</returns>
    public static int ReverseBits(int value)
    {
        uint n = (uint)value;
        n = (n >> 1) & 0x55555555 | (n << 1) & 0xaaaaaaaa;
        n = (n >> 2) & 0x33333333 | (n << 2) & 0xcccccccc;
        n = (n >> 4) & 0x0f0f0f0f | (n << 4) & 0xf0f0f0f0;
        n = (n >> 8) & 0x00ff00ff | (n << 8) & 0xff00ff00;
        n = (n >> 16) & 0x0000ffff | (n << 16) & 0xffff0000;
        return (int)n;
    }
    
    /// <summary>
    ///     Counts the number of set bits in an integer.
    /// </summary>
    /// <param name="i">Integer</param>
    /// <returns>Number of set bits</returns>
    public static int SumoAnimCountBits32(int i)
    {
        return BitOperations.PopCount((uint)i);
    }
    
    /// <summary>
    ///     Gets the number of bits stored in a keyframe.
    /// </summary>
    /// <param name="bitpack">Bit descriptor for keyframe</param>
    /// <returns>Number of bits in the keyframe</returns>
    public static int SumoAnimGetStrideFromBitPacked(int bitpack)
    {
        uint u = (uint)bitpack;
        return (int)(((u >> 0x16 & 0x1f) + (u >> 0xc & 0x1f) + (u >> 0x1b & 0xf) +
                      (u >> 2 & 0x1f) + (u >> 0x11 & 0xf) + (u >> 7 & 0xf) +
                      (u >> 0x15 & 1) + (u >> 0xb & 1)) - ((int)u >> 0x1f));
    }

    public static Quaternion DecompressSmallest3(ArraySegment<byte> buffer, int offset)
    {
        const float quatScale = 23169.77f;
        const float quatOffset = quatScale / 1.414214f;
        
        int v = buffer.ReadInt32BigEndian(offset + 2);
                    
        float x = buffer.ReadInt16(offset) & 0x7fff;
        float y = v >>> 0x11;
        float z = (v >>> 2) & 0x7fff;
                    
        x = (x - quatOffset) / quatScale;
        y = (y - quatOffset) / quatScale;
        z = (z - quatOffset) / quatScale;
        
        float w = (float)Math.Sqrt(1.0 - (Math.Pow(x, 2.0) + Math.Pow(y, 2.0) + Math.Pow(z, 2.0)));
                    
        Quaternion q = (v & 3) switch
        {
            0 => new Quaternion(w, x, y, z),
            1 => new Quaternion(x, w, y, z),
            2 => new Quaternion(x, y, w, z),
            3 => new Quaternion(x, y, z, w),
            _ => throw new Exception("impossible to hit this")
        };
        
        return q;
    }
    
    public static float DecompressValueBitPacked(int header, ArraySegment<byte> buffer, ref int offset)
    {
        int numSignBits = (header >>> 0x1f); 
        int numExponentBits = header >>> 0x1b & 0xf; // header >> 0x1b & 0xf
        int numMantissaBits = header >>> 0x16 & 0x1f; // header >> 0x16 & 0x1f
        int numBits = numSignBits + numExponentBits + numMantissaBits;
        
        // worst way of doing this
        int byteOffset = offset >>> 3;
        int bitOffset = offset & 7;
        offset += numBits;
        
        int remaining = 8 - bitOffset;
        byte iterator = buffer[byteOffset++];

        int significant = 0;
        int exponent = 0;
        int sign = 0;

        for (int i = 0; i < numMantissaBits; ++i)
        {
            significant |= (((iterator >>> (remaining - 1)) & 1) << i);
            
            remaining -= 1;
            if (remaining != 0) continue;
            remaining = 8;
            iterator = buffer[byteOffset++];
        }
        
        for (int i = 0; i < numExponentBits; ++i)
        {
            exponent |= (((iterator >>> (remaining - 1)) & 1) << i);
            
            remaining -= 1;
            if (remaining != 0) continue;
            remaining = 8;
            iterator = buffer[byteOffset++];
        }
        
        
        for (int i = 0; i < numSignBits; ++i)
        {
            sign |= (((iterator >>> (remaining - 1)) & 1) << i);
            
            remaining -= 1;
            if (remaining != 0) continue;
            remaining = 8;
            iterator = buffer[byteOffset++];
        }
        
        
        if (numExponentBits == 0)
        {
            if (numMantissaBits == 0) return 0.0f;
            
            float mantissa = MaskTable[numMantissaBits];
            if (numSignBits == 0) return significant / mantissa;
            
            float v = significant;
            if (sign != 0)
                v = SignExtendTable[numMantissaBits] | significant;
                
            return v / mantissa;
        }
        
        // mantissa
        // exponent
        // sign

        exponent += ExponentBiasTable[numExponentBits];
        if (numMantissaBits < 0x18) significant <<= (0x17 - numMantissaBits);
        else significant >>= (numMantissaBits - 0x17);
        
        if (exponent < 0)
            return 0.0f;
        
        if (exponent > 0xff)
        {
            exponent = 0xff;
            significant = 0x7fffff;
        }
        
        float value = BitConverter.Int32BitsToSingle(significant | sign << 0x1f | exponent << 0x17);
        Console.WriteLine($"Sign={sign:x8}, Exponent={exponent:x8}, Fraction={significant:x8}, Value = {value}");
        
        return BitConverter.Int32BitsToSingle(significant | sign << 0x1f | exponent << 0x17);
    }
    
    
    // public static float DecompressValueBitPacked(int header, ArraySegment<byte> buffer, ref int offset)
    // {
    //     int numSignBits = (header >>> 0x1f); 
    //     int numExponentBits = header >>> 0x1b & 0xf; // header >> 0x1b & 0xf
    //     int numMantissaBits = header >>> 0x16 & 0x1f; // header >> 0x16 & 0x1f
    //     
    //     int b = offset >>> 3;
    //     
    //     uint bits = 0;
    //     if (b + 4 < buffer.Count) bits |= (uint)(buffer[b + 4] << (32 - (offset & 7)));
    //     if (b + 3 < buffer.Count) bits |= (uint)((buffer[b + 3] << 24) >>> (offset & 7));
    //     if (b + 2 < buffer.Count) bits |= (uint)(buffer[b + 2] << 16);
    //     if (b + 1 < buffer.Count) bits |= (uint)(buffer[b + 1] << 8);
    //     if (b < buffer.Count) bits |= (buffer[b + 0]);
    //     
    //     offset += numSignBits + numExponentBits + numMantissaBits;
    //     
    //     if (numExponentBits == 0)
    //     {
    //         if (numMantissaBits == 0) return 0.0f;
    //         if (numSignBits == 0 || (bits & SignMaskTable[numMantissaBits]) == 0)
    //             return (float)(bits & MaskTable[numMantissaBits]) / (uint)MaskTable[numMantissaBits];
    //         
    //         return -(float)(uint)-(bits | (uint)SignExtendTable[numMantissaBits]) / (uint)MaskTable[numMantissaBits];
    //     }
    //     
    //     int sign = 0, exponent = 0, fraction = 0;
    //
    //     sign = (int)(MaskTable[numSignBits] & bits >> numMantissaBits + numExponentBits);
    //     exponent = (int)((MaskTable[numExponentBits] & bits >> numMantissaBits) + ExponentBiasTable[numExponentBits]);
    //     if (numMantissaBits < 0x18) fraction = (int) ((bits & MaskTable[numMantissaBits]) << (0x17 - numMantissaBits));
    //     else fraction = (int) ((bits & MaskTable[numMantissaBits]) >> (numMantissaBits - 0x17));
    //     
    //     if (exponent > 0xff)
    //     {
    //         exponent = 0xff;
    //         fraction = 0x7fffff;
    //     }
    //     else if (exponent < 0) sign = exponent = fraction = 0;
    //     
    //     float value = BitConverter.Int32BitsToSingle(fraction | sign << 0x1f | exponent << 0x17);
    //     Console.WriteLine($"Sign={sign:x8}, Exponent={exponent:x8}, Fraction={fraction:x8}, Value = {value}");
    //     
    //     return BitConverter.Int32BitsToSingle(fraction | sign << 0x1f | exponent << 0x17);
    // }
    
    
    
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
    
    /// <summary> 
    /// Convert a 10 byte signed integer to float.
    /// Expected format: bits are contained in 0x3FF.
    /// </summary>
    public static float DenormalizeSigned10BitInt(ushort value)
    {
        float result;
        if ((value & 0x200) != 0)
        {
            //Two's complement conversion for 10 bit integer

            value = (ushort)~value;              //Invert bits
            value = (ushort)(value & 0x3FF);     //Apply only the first 10 bits
            value = (ushort)(value + 1);         // +1
            result = -value;
        }
        else
            result = (ushort)(value & 0x1FF);     //Apply mask
                
            
        return result / 511.0f;
    }
    
    /// <summary>
    /// Convert a 3 bit int into an unsigned floating point number
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static float DenormalizeUnsigned3BitInt(byte value)
    {
        return value / 3.0f;
    }
}