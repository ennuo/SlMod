namespace SlLib.SumoTool.Siff.Forest.DirectX.Xbox;

public struct XboxVertexElement
{
    public short Stream;
    public short Offset;
    public XboxDeclType Type;
    public D3DDECLMETHOD Method;
    public D3DDECLUSAGE Usage;
    public byte UsageIndex;

    public static int GetTypeSize(XboxDeclType type)
    {
        return type switch
        {
            XboxDeclType.FLOAT2 => 0x8,
            XboxDeclType.INT2 => 0x8,
            XboxDeclType.INT2N => 0x8,
            XboxDeclType.UINT2 => 0x8,
            XboxDeclType.UINT2N => 0x8,
            XboxDeclType.FLOAT3 => 0xc,
            XboxDeclType.FLOAT4 => 0x10,
            XboxDeclType.INT4 => 0x10,
            XboxDeclType.INT4N => 0x10,
            XboxDeclType.UINT4 => 0x10,
            XboxDeclType.UINT4N => 0x10,
            XboxDeclType.SHORT4 => 0x8,
            XboxDeclType.SHORT4N => 0x8,
            XboxDeclType.USHORT4 => 0x8,
            XboxDeclType.USHORT4N => 0x8,
            XboxDeclType.FLOAT16x4 => 0x8,
            _ => 0x4
        };
    }

    public static D3DDECLTYPE MapTypeToD3D9(XboxDeclType type)
    {
        return type switch
        {
            XboxDeclType.FLOAT1 => D3DDECLTYPE.FLOAT1,
            XboxDeclType.FLOAT2 => D3DDECLTYPE.FLOAT2,
            XboxDeclType.FLOAT3 => D3DDECLTYPE.FLOAT3,
            XboxDeclType.FLOAT4 => D3DDECLTYPE.FLOAT4,
            XboxDeclType.D3DCOLOR => D3DDECLTYPE.D3DCOLOR,
            XboxDeclType.UBYTE4 => D3DDECLTYPE.UBYTE4,
            XboxDeclType.SHORT2 => D3DDECLTYPE.SHORT2,
            XboxDeclType.SHORT4 => D3DDECLTYPE.SHORT4,
            XboxDeclType.UBYTE4N => D3DDECLTYPE.UBYTE4N,
            XboxDeclType.BYTE4N => D3DDECLTYPE.UBYTE4N,
            XboxDeclType.SHORT2N => D3DDECLTYPE.SHORT2N,
            XboxDeclType.SHORT4N => D3DDECLTYPE.SHORT4N,
            XboxDeclType.USHORT2N => D3DDECLTYPE.USHORT2N,
            XboxDeclType.USHORT4N => D3DDECLTYPE.USHORT4N,
            XboxDeclType.UDEC3 => D3DDECLTYPE.UDEC3,
            XboxDeclType.DEC3N => D3DDECLTYPE.DEC3N,
            XboxDeclType.FLOAT16x2 => D3DDECLTYPE.FLOAT16x2,
            XboxDeclType.FLOAT16x4 => D3DDECLTYPE.FLOAT16x4,
            
            _ => throw new ArgumentException("Invalid format!")
        };
    }
}