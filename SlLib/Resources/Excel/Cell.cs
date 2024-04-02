namespace SlLib.Resources.Excel;

/// <summary>
///     Represents a cell in a column.
/// </summary>
/// <param name="name">The hash of the name of the cell</param>
/// <param name="type">The hash of the type of the cell</param>
/// <param name="value">The value to be held by the cell</param>
public class Cell(int name, int type, object value)
{
    /// <summary>
    ///     The hash of the name of this cell.
    /// </summary>
    public readonly int Name = name;

    /// <summary>
    ///     The hash of the type of this cell.
    /// </summary>
    public readonly int Type = type;

    /// <summary>
    ///     The value held by this cell.
    /// </summary>
    public object Value = value;

    public bool IsInteger()
    {
        return Type == CellType.Int;
    }

    public bool IsUnsignedInteger()
    {
        return Type == CellType.Uint;
    }

    public bool IsString()
    {
        return Type == CellType.String;
    }

    public bool IsFloat()
    {
        return Type == CellType.Float;
    }
}