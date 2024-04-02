using SlLib.Extensions;

namespace SlLib.Resources.Excel;

/// <summary>
///     Represents a binary spreadsheet.
/// </summary>
public class ExcelData
{
    /// <summary>
    ///     The worksheets in this spreadsheet.
    /// </summary>
    public List<Worksheet> Worksheets = [];

    /// <summary>
    ///     Gets a worksheet by name
    /// </summary>
    /// <param name="name">The name of the worksheet to find</param>
    /// <returns>The worksheet, if found</returns>
    public Worksheet? GetWorksheet(string name)
    {
        return Worksheets.Find(worksheet => worksheet.Name == name);
    }

    /// <summary>
    ///     Loads a spreadsheet from a buffer.
    /// </summary>
    /// <param name="data">Buffer to parse excel data from</param>
    /// <returns>Parsed excel data instance</returns>
    public static ExcelData Load(byte[] data)
    {
        if (data.Length < 8 || data.ReadInt32(0) != 0x54525353 /* SSRT */)
            throw new ArgumentException("Invalid excel data header!");

        var excel = new ExcelData();

        int count = data.ReadInt32(4);
        for (int i = 0; i < count; ++i)
        {
            int address = data.ReadInt32(0x8 + i * 0x4);
            excel.Worksheets.Add(Worksheet.Load(data, address));
        }

        return excel;
    }
}