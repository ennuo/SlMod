using SlLib.Utilities;

namespace SlLib.Resources.Excel;

/// <summary>
///     Supported data types by cells in excel data.
/// </summary>
public static class CellType
{
    public static readonly int Float = SlUtil.SumoHash("float");
    public static readonly int String = SlUtil.SumoHash("string");
    public static readonly int Int = SlUtil.SumoHash("int");
    public static readonly int Uint = SlUtil.SumoHash("uint");
}