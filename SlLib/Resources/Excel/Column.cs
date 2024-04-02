using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Utilities;

namespace SlLib.Resources.Excel;

public class Column
{
    /// <summary>
    ///     The cells in this column.
    /// </summary>
    public List<Cell> Cells = [];

    /// <summary>
    ///     The name of this column.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     Gets a cell by its name.
    /// </summary>
    /// <param name="name">The name of the cell to find</param>
    /// <returns>The cell, if found</returns>
    public Cell? GetCellByName(string name)
    {
        return GetCellByName(SlUtil.SumoHash(name));
    }

    /// <summary>
    ///     Gets a cell by its name hash.
    /// </summary>
    /// <param name="name">The hash of the name of the cell to find</param>
    /// <returns>The cell, if found</returns>
    public Cell? GetCellByName(int name)
    {
        return Cells.Find(cell => cell.Name == name);
    }

    /// <summary>
    ///     Loads a column from a buffer.
    /// </summary>
    /// <param name="data">Buffer to parse column from</param>
    /// <param name="offset">Offset into buffer to parse from</param>
    /// <returns>Parsed column instance</returns>
    /// <exception cref="SerializationException">Thrown if invalid cell type is encountered</exception>
    public static Column Load(byte[] data, int offset)
    {
        var column = new Column
        {
            // Hash = data.ReadInt32(offset + 4),
            Name = data.ReadString(offset + 8)
        };

        int count = data.ReadInt32(offset);
        for (int i = 0; i < count; ++i)
        {
            int address = offset + 0x48 + i * 0xc;

            int name = data.ReadInt32(address);
            int type = data.ReadInt32(address + 4);
            object value;

            if (type == CellType.String) value = data.ReadString(data.ReadInt32(address + 8));
            else if (type == CellType.Int) value = data.ReadInt32(address + 8);
            else if (type == CellType.Uint) value = data.ReadInt32(address + 8);
            else if (type == CellType.Float) value = data.ReadFloat(address + 8);
            else throw new SerializationException("Invalid data type found in column data!");

            column.Cells.Add(new Cell(name, type, value));
        }

        return column;
    }
}