using SlLib.Extensions;

namespace SlLib.Excel;

/// <summary>
///     A worksheet consisting of columns and cells.
/// </summary>
public class Worksheet
{
    /// <summary>
    ///     The columns in this worksheet.
    /// </summary>
    public List<Column> Columns = [];

    /// <summary>
    ///     The name of this worksheet.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     Gets a column by name.
    /// </summary>
    /// <param name="name">The name of the column to find</param>
    /// <returns>The column, if found</returns>
    public Column? GetColumnByName(string name)
    {
        return Columns.Find(column => column.Name == name);
    }

    /// <summary>
    ///     Loads a worksheet from a buffer.
    /// </summary>
    /// <param name="data">Buffer to parse worksheet from</param>
    /// <param name="offset">Offset into buffer to parse from</param>
    /// <returns>Parsed worksheet instance</returns>
    public static Worksheet Load(byte[] data, int offset)
    {
        var worksheet = new Worksheet
        {
            // Hash = data.ReadInt32(offset + 0),
            Name = data.ReadString(offset + 4)
        };

        int count = data.ReadInt32(offset + 0x44);
        for (int i = 0; i < count; ++i)
        {
            int address = data.ReadInt32(offset + 0x48 + i * 4);
            worksheet.Columns.Add(Column.Load(data, address));
        }

        return worksheet;
    }
}