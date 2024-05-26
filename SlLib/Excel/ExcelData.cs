using SlLib.Extensions;
using SlLib.Utilities;

namespace SlLib.Excel;

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

    /// <summary>
    ///     Saves this spreadsheet to a buffer.
    /// </summary>
    /// <returns>Serialized spreadsheet</returns>
    public byte[] save()
    {
        int size = SlUtil.Align(0x8 + (0x4 * Worksheets.Count), 0x10);
        byte[] buffer = new byte[size];
        buffer.WriteInt32(0x54525353, 0x0); // SSRT
        buffer.WriteInt32(Worksheets.Count, 0x4);

        int array = 0x8;
        for (int i = 0; i < Worksheets.Count; ++i)
        {
            Worksheet worksheet = Worksheets[i];
            // buffer.WriteInt32(buffer.Length, );
            
        }
        
        return Array.Empty<byte>();
    }
}