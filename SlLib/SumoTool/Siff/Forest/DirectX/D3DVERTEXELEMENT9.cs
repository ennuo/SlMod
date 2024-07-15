namespace SlLib.SumoTool.Siff.Forest.DirectX;

public struct D3DVERTEXELEMENT9
{
    public short Stream;
    public short Offset;
    public D3DDECLTYPE Type;
    public D3DDECLMETHOD Method;
    public D3DDECLUSAGE Usage;
    public byte UsageIndex;
    
    public static int GetTypeSize(D3DDECLTYPE type)
    {
        return type switch
        {
            D3DDECLTYPE.FLOAT2 => 0x8,
            D3DDECLTYPE.FLOAT3 => 0xc,
            D3DDECLTYPE.FLOAT4 => 0x10,
            D3DDECLTYPE.SHORT4 => 0x8,
            D3DDECLTYPE.SHORT4N => 0x8,
            D3DDECLTYPE.USHORT4N => 0x8,
            D3DDECLTYPE.FLOAT16x4 => 0x8,
            _ => 0x4
        };
    }
}