﻿namespace SlLib.Utilities;

/// <summary>
///     Common crypto utilities
/// </summary>
public static class CryptUtil
{
    /// <summary>
    ///     Key used for decoding/encoding zat buffers.
    /// </summary>
    private static readonly byte[] ShuffleKey =
    [
        0xff, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde,
        0xbb, 0xac, 0x9d, 0x8e, 0x7f, 0x60, 0x51, 0x42
    ];

    /// <summary>
    ///     Header for munged TOC files, used for verification.
    /// </summary>
    private static readonly byte[] AndroidPackFileMungedIdentity =
    [
        0x88, 0xBA, 0x00, 0x7E, 0x69, 0x46, 0xDA, 0xD0,
        0xFE, 0x97, 0x01, 0xA6, 0x96, 0xBB, 0x5B, 0x1A
    ];

    private static readonly byte[] Ps3PackFileMungedIdentity =
    [
        0x7e, 0x00, 0xba, 0x88, 0xd0, 0xda, 0x46, 0x69,
        0xa6, 0x01, 0x97, 0xfe, 0x1a, 0x5b, 0xbb, 0x96
    ];
    
    /// <summary>
    ///     Key used for decoding/encoding TOC files.
    /// </summary>
    private static readonly byte[] PackFileMungeKey =
    [
        0xB0, 0x11, 0x0B, 0x70, 0x4A, 0x25, 0x9C, 0xD8, 0xF0, 0xDA, 0x9E, 0x44, 0x80, 0xBD, 0x0B, 0x9C, 0x8C, 0x7C,
        0x94, 0xC6, 0x09, 0xF3, 0x22, 0x28, 0x82, 0x73, 0xCF, 0x85, 0x93, 0x0D, 0xD6, 0x4F,
        0x7B, 0xAF, 0x13, 0x25, 0x5B, 0x07, 0x5D, 0xF9, 0x12, 0xE8, 0x1C, 0x2D, 0x8A, 0xAE, 0x98, 0xFE, 0x03, 0x4D,
        0xEE, 0x5F, 0x61, 0xC2, 0x32, 0xC4, 0xB3, 0x92, 0x80, 0xFE, 0x4C, 0x68, 0xC4, 0x61,
        0x51, 0xBE, 0x7F, 0x49, 0xB7, 0xCB, 0xD4, 0xEE, 0x09, 0x22, 0xAC, 0xB0, 0x2A, 0xAA, 0x76, 0xBC, 0x95, 0x85,
        0xDF, 0xB8, 0x9C, 0x5F, 0xB9, 0x0D, 0xA7, 0x09, 0x42, 0x89, 0xEA, 0x18, 0xC8, 0x13,
        0x12, 0x83, 0x9B, 0x06, 0xFD, 0x26, 0x72, 0x73, 0xC8, 0x1B, 0xAB, 0xEA, 0x38, 0x40, 0x52, 0x76, 0xA1, 0xD3,
        0x58, 0x35, 0x91, 0x6E, 0x7C, 0xEC, 0x88, 0xF2, 0x64, 0x73, 0xAB, 0xB7, 0x95, 0x78,
        0x76, 0x2E, 0x3A, 0xBE, 0x0C, 0xE7, 0x19, 0x30, 0x91, 0x14, 0xAA, 0x40, 0xDC, 0xF8, 0xD7, 0x46, 0x6E, 0x4E,
        0xDC, 0x1B, 0x8A, 0x88, 0x7D, 0x1E, 0xD7, 0xFA, 0xD0, 0xC7, 0xD4, 0x18, 0xFE, 0x81,
        0x66, 0x0E, 0xA5, 0xB9, 0x44, 0x5D, 0x2D, 0xB4, 0x71, 0x74, 0xCC, 0x1B, 0xFA, 0xA2, 0xC9, 0x6E, 0xB8, 0x4C,
        0xCA, 0x8E, 0x44, 0x7C, 0x95, 0xEF, 0x12, 0xA7, 0x6E, 0x74, 0x5C, 0x7D, 0x19, 0x49,
        0x7D, 0x47, 0x29, 0xBE, 0xAB, 0xA2, 0x44, 0xEB, 0x99, 0x2F, 0x2E, 0x46, 0x93, 0x49, 0x20, 0xCA, 0xA5, 0xAD,
        0x09, 0xD6, 0x95, 0x0E, 0x0F, 0x9F, 0xEF, 0xDA, 0xC6, 0x10, 0x4B, 0x56, 0x09, 0x37,
        0x9E, 0xFF, 0x0A, 0xD4, 0xDE, 0x56, 0xA9, 0x41, 0xE6, 0x3B, 0xAF, 0x25, 0x92, 0x4D, 0xCD, 0xCB, 0x56, 0x32,
        0xD2, 0x7B, 0x58, 0x10, 0x8D, 0x5A, 0xF1, 0x9E, 0xA2, 0xB3, 0x94, 0x69, 0x86, 0x51,
        0x49, 0x40, 0xBD, 0x49, 0x53, 0x39, 0x63, 0x91, 0x45, 0x02, 0x86, 0xAC, 0x8F, 0x47, 0xE0, 0x7B, 0x95, 0xA8,
        0xAB, 0x59, 0xA7, 0x70, 0xAC, 0x71, 0xAC, 0xBF, 0x51, 0xBA, 0x9C, 0x8F, 0x4C, 0x18,
        0xFB, 0x88, 0x86, 0x5F, 0x1D, 0x49, 0x57, 0x7F, 0xD1, 0x4D, 0xA8, 0x19, 0x22, 0x3D, 0x4C, 0x78, 0xA4, 0x9F,
        0x88, 0x8D, 0x78, 0x14, 0x1B, 0xFA, 0x14, 0x06, 0x69, 0xED, 0xA3, 0xD8, 0x90, 0xE0,
        0x5F, 0x6D, 0x85, 0xB8, 0x05, 0x80, 0x42, 0xB1, 0x27, 0xE5, 0x0F, 0xEB, 0x1A, 0x6F, 0xDF, 0x28, 0xA3, 0x09,
        0x30, 0x96, 0x46, 0xFF, 0x30, 0x81, 0x93, 0x3D, 0x93, 0x29, 0x32, 0x39, 0xDE, 0x9C,
        0x74, 0xC5, 0x5F, 0x05, 0x89, 0xB3, 0x65, 0x8B, 0xB2, 0x76, 0x48, 0x33, 0xA1, 0x00, 0x1E, 0xC8, 0xC6, 0xDC,
        0xBA, 0x7C, 0x61, 0x0D, 0x15, 0xDF, 0x27, 0xFD, 0x79, 0x38, 0xDA, 0x58, 0xEA, 0x7C,
        0x87, 0x7C, 0x0F, 0xD3, 0x8C, 0x26, 0x7C, 0xA2, 0x95, 0xB5, 0x0E, 0x37, 0x56, 0x0C, 0x70, 0x2B, 0x1A, 0x8F,
        0x0F, 0xA9, 0x22, 0x7E, 0xCC, 0xBB, 0xC2, 0xB0, 0xD6, 0xE7, 0x04, 0xB8, 0xA1, 0xBD,
        0x65, 0xA3, 0xFB, 0x42, 0x25, 0x05, 0x57, 0xC8, 0x67, 0x5B, 0x1F, 0x9E, 0xBD, 0x6C, 0xE0, 0x56, 0xFB, 0xF0,
        0x09, 0x8E, 0xE0, 0x3E, 0x3B, 0xA7, 0x5F, 0xC2, 0x9D, 0xBC, 0x92, 0x17, 0xDE, 0x62,
        0xB6, 0x7A, 0x04, 0xF8, 0x2A, 0xBB, 0x8B, 0x8B, 0x2E, 0xCA, 0x5E, 0xE0, 0xEE, 0x89, 0x21, 0x98, 0x05, 0x28,
        0x1B, 0x56, 0xC0, 0xBE, 0xB6, 0xDB, 0x52, 0x69, 0xC4, 0x5C, 0xF3, 0x30, 0xCB, 0xD7,
        0xE1, 0xA7, 0x57, 0x94, 0x56, 0xE3, 0x80, 0xE7, 0x02, 0x7E, 0xF7, 0x7B, 0x4F, 0xB6, 0xFD, 0x2D, 0x9D, 0xF0,
        0x02, 0x48, 0x35, 0x7E, 0x72, 0xB2, 0x06, 0xED, 0x8D, 0x6D, 0xB3, 0x57, 0x7A, 0x7C,
        0xF8, 0x1C, 0x04, 0xDB, 0xF0, 0x54, 0xFF, 0xBE, 0x69, 0xB2, 0xCE, 0xDE, 0x6C, 0xA2, 0x66, 0x17, 0x2A, 0x13,
        0x87, 0x54, 0xB5, 0x88, 0xB4, 0x8F, 0xF6, 0x86, 0xC7, 0x28, 0x83, 0x8C, 0x4C, 0x9C,
        0x43, 0x42, 0x04, 0x5F, 0xD7, 0x4E, 0x68, 0xF2, 0x8B, 0x7D, 0x0F, 0x86, 0xA5, 0xBF, 0xBE, 0xE8, 0x6D, 0x51,
        0x22, 0xB8, 0x38, 0xAE, 0xAB, 0x59, 0xCD, 0xDF, 0x8A, 0xCC, 0x7D, 0x71, 0x41, 0x6F,
        0x22, 0x39, 0xD2, 0xA2, 0x2D, 0xB3, 0xEE, 0x83, 0xCD, 0x03, 0x96, 0x08, 0x9D, 0xBA, 0xE3, 0xBF, 0x9F, 0x7B,
        0xF7, 0x08, 0x1E, 0xC8, 0x59, 0x1C, 0x15, 0x95, 0x07, 0xD0, 0xA1, 0xD2, 0xB8, 0xD3,
        0x4F, 0x47, 0x7C, 0x45, 0xDF, 0xA2, 0x34, 0x17, 0x16, 0xBE, 0x64, 0xAB, 0x44, 0x5B, 0xEF, 0x84, 0x94, 0xFB,
        0x8F, 0x2A, 0xA9, 0xB5, 0xDD, 0x60, 0xD1, 0xE6, 0x1A, 0x22, 0xE5, 0xB4, 0x4F, 0x73,
        0xDF, 0xCA, 0xBF, 0x40, 0x7B, 0xA0, 0xE3, 0x5F, 0x1A, 0x95, 0xD6, 0x9C, 0xD7, 0x3E, 0x42, 0xA3, 0xE9, 0xD9,
        0x66, 0xBF, 0x43, 0x9C, 0x3B, 0x69, 0xFB, 0x82, 0xDF, 0x49, 0x29, 0xA8, 0xD6, 0x5F,
        0xA7, 0x34, 0xDC, 0xF3, 0x9C, 0x73, 0x80, 0x53, 0xBB, 0x17, 0xF3, 0x29, 0x6B, 0x27, 0x0B, 0x3A, 0x85, 0x99,
        0x70, 0x08, 0x3B, 0x9E, 0xB0, 0xB9, 0x73, 0x2F, 0x87, 0xF9, 0x65, 0x02, 0xC4, 0x43,
        0x32, 0x79, 0x02, 0xB1, 0x48, 0x27, 0x05, 0x6B, 0x37, 0x11, 0xC5, 0x51, 0x8D, 0x8B, 0x2E, 0xF2, 0x50, 0xF4,
        0xA9, 0x58, 0xD6, 0x1F, 0xA8, 0x9A, 0xDE, 0x5C, 0x2A, 0xF1, 0x75, 0x9A, 0x57, 0xA2,
        0x40, 0x3D, 0xAD, 0x07, 0xC1, 0x61, 0x1B, 0xFD, 0x99, 0x73, 0x6A, 0xC5, 0xC8, 0x6B, 0x90, 0x79, 0xB6, 0x76,
        0x40, 0x1E, 0xA1, 0x0E, 0x6D, 0xD5, 0xFB, 0xF3, 0x07, 0x8E, 0x1A, 0xE6, 0x72, 0x3B,
        0x63, 0xF4, 0xD3, 0x90, 0x8C, 0x0B, 0x9B, 0x7F, 0x67, 0x9B, 0x3B, 0x25, 0xAE, 0x7E, 0x7B, 0x98, 0xBA, 0x9C,
        0x12, 0x79, 0x36, 0xBA, 0x9A, 0x0E, 0x86, 0x46, 0x6E, 0xDB, 0x58, 0x06, 0x81, 0xE4,
        0xF9, 0x2F, 0x1B, 0x8B, 0x0C, 0x91, 0xD2, 0xCA, 0x57, 0xA6, 0x5C, 0x98, 0xF0, 0x3F, 0x69, 0xC8, 0x94, 0x6F,
        0xD9, 0x3C, 0x50, 0x51, 0xCF, 0x52, 0x10, 0xAA, 0x5E, 0x7D, 0x9A, 0x13, 0xE5, 0x8C,
        0x46, 0x41, 0x70, 0xCC, 0x6D, 0x77, 0x0E, 0x30, 0x78, 0xDF, 0xDE, 0x66, 0x02, 0xC7, 0xC6, 0x52, 0x6F, 0x21,
        0x6C, 0xBB, 0x6B, 0x28, 0xDB, 0x3C, 0x8A, 0x44, 0x25, 0x46, 0x09, 0x23, 0x42, 0x53,
        0x1B, 0x87, 0x5C, 0x88, 0x7D, 0x66, 0xF5, 0x77, 0x22, 0xE7, 0xB8, 0x50, 0x16, 0x14, 0x83, 0xC1, 0x8A, 0xC2,
        0xAB, 0x6C, 0xEA, 0x7F, 0xB6, 0x7F, 0x05, 0xF5, 0x63, 0x6A, 0x27, 0x76, 0xC0, 0xDE,
        0xE8, 0x09, 0x13, 0xA8, 0xCE, 0xC0, 0x42, 0xF9, 0xD4, 0x8E, 0x4D, 0xB7, 0x93, 0x65, 0xCF, 0x0C, 0x36, 0xF6,
        0xC6, 0x44, 0x63, 0x28, 0x31, 0x11, 0x34, 0xB1, 0x67, 0x72, 0x18, 0x54, 0x9F, 0x65,
        0xBC, 0xD2, 0xE7, 0xAC, 0xE3, 0xA3, 0x03, 0x25, 0x0A, 0x66, 0xCB, 0x94, 0x2D, 0xFD, 0x4D, 0x23, 0x4D, 0x20,
        0xEF, 0x14, 0x31, 0x5A, 0xD5, 0xC4, 0x60, 0xA4, 0x60, 0xFA, 0x26, 0xF5, 0x6E, 0xFE,
        0x98, 0x44, 0x8B, 0x72, 0xAB, 0x37, 0x4E, 0x7F, 0xC6, 0xDE, 0x47, 0x5B, 0x80, 0x1F, 0xA3, 0xBA, 0x1F, 0x05,
        0x62, 0x37, 0x08, 0x94, 0xE0, 0x81, 0x5F, 0x54, 0x5A, 0x6C, 0x67, 0x1A, 0x5B, 0x0D,
        0x89, 0xFC, 0x9E, 0xC9, 0x83, 0xE2, 0x6A, 0xEA, 0x00, 0xE1, 0xB1, 0x40, 0x7E, 0x4D, 0x27, 0x8D, 0xE7, 0x4B,
        0x31, 0xD5, 0xAA, 0xAE, 0x78, 0xA5, 0xED, 0xBC, 0x43, 0x90, 0x11, 0xCE, 0xB2, 0xDB
    ];

    /// <summary>
    ///     Encodes a dat buffer in-place to zat.
    /// </summary>
    /// <param name="buf">Buffer to encode</param>
    public static void EncodeBuffer(ArraySegment<byte> buf)
    {
        int len = buf.Count;
        int half = buf.Count >> 1;

        if ((len & 1) != 0) buf[half] += ShuffleKey[half % 0x10];
        for (int i = 0, j = len - 1; i < half; ++i, --j)
        {
            byte swap = (byte)(buf[i] + ShuffleKey[j % 0x10]);
            buf[i] = (byte)(buf[j] + ShuffleKey[i % 0x10]);
            buf[j] = swap;
        }

        for (int i = 0; i < half; ++i) (buf[i], buf[i + half]) = (buf[i + half], buf[i]);
    }

    /// <summary>
    ///     Decodes a zat buffer in-place to dat.
    /// </summary>
    /// <param name="buf">Buffer to decode</param>
    public static void DecodeBuffer(ArraySegment<byte> buf)
    {
        int len = buf.Count;
        int half = buf.Count >> 1;

        for (int i = 0; i < half; ++i) (buf[i], buf[i + half]) = (buf[i + half], buf[i]);

        for (int i = 0, j = len - 1; i < half; ++i, --j)
        {
            byte swap = (byte)(buf[i] - ShuffleKey[i % 0x10]);
            buf[i] = (byte)(buf[j] - ShuffleKey[j % 0x10]);
            buf[j] = swap;
        }

        if ((len & 1) != 0) buf[half] -= ShuffleKey[half % 0x10];
    }

    /// <summary>
    ///     Unmunges a pack file buffer in-place.
    /// </summary>
    /// <param name="buffer">Buffer to unmunge</param>
    /// <exception cref="ArgumentException">Thrown if TOC buffer is too small</exception>
    public static void PackFileUnmunge(ArraySegment<byte> buffer)
    {
        if (buffer.Count < 16)
            throw new ArgumentException("TOC buffer must be at least 16 bytes to verify identity!");

        var header = buffer[..16];
        if (!header.SequenceEqual(AndroidPackFileMungedIdentity)) return;

        for (int i = 0; i < buffer.Count; ++i)
        {
            byte key = PackFileMungeKey[i % 0x400];
            buffer[i] = (byte)(buffer[i] ^ ((key << 4) | (key >> 4)));
        }
    }
}