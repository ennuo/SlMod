using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using SlLib.Excel;
using SlLib.Lookup;
using Cell = SlLib.Excel.Cell;

namespace SlToolkit.UI;

internal class CellModel : INotifyPropertyChanged
{
    public CellModel(Cell cell)
    {
        Cell = cell;

        Name = ExcelPropertyNameLookup.GetPropertyName(cell.Name) ?? ((uint)cell.Name).ToString();

        if (cell.Type == CellType.Float) Type = typeof(float);
        else if (cell.Type == CellType.Int) Type = typeof(int);
        else if (cell.Type == CellType.String) Type = typeof(string);
        else if (cell.Type == CellType.Uint) Type = typeof(uint);
    }

    public string Name { get; }
    public Type Type { get; }
    public Cell Cell { get; }

    public object Value
    {
        get => Cell.Value;
        set
        {
            if (value == Cell.Value) return;
            Cell.Value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class MainForm : Form
{
    public MainForm()
    {
        Title = "Sumo Toolkit";
        ClientSize = new Size(854, 480);


        ExcelData data = ResourceManager.Instance.RacerData;
        Worksheet racers = data.GetWorksheet("Racers")!;

        var infoColumn = new GridColumn
        {
            HeaderText = "Name",
            Editable = false,
            DataCell = new TextBoxCell { Binding = Binding.Property((CellModel cell) => cell.Name + "") }
        };

        var valueProperty = Binding.Property((CellModel m) => m.Value);
        var propertyCell = new PropertyCell
        {
            TypeBinding = Binding.Property((CellModel m) => (object)m.Type),
            Types =
            {
                new PropertyCellTypeString { ItemBinding = valueProperty.OfType<string>() },
                new PropertyCellTypeNumber<float> { ItemBinding = valueProperty.OfType<float>() },
                new PropertyCellTypeNumber<int> { ItemBinding = valueProperty.OfType<int>() },
                new PropertyCellTypeNumber<uint> { ItemBinding = valueProperty.OfType<uint>() }
            }
        };

        var filtered = racers.GetColumnByName("sonic")!.Cells.Select(cell => new CellModel(cell)).ToList();

        var valueColumn = new GridColumn
        {
            AutoSize = true,
            Editable = true,
            HeaderText = "Value",
            DataCell = propertyCell
        };

        var grid = new GridView { Columns = { infoColumn, valueColumn } };
        grid.DataStore = filtered;


        var list = new ListBox
        {
            Size = new Size(-1, 200)
        };

        foreach (Column column in racers.Columns) list.Items.Add(new ListItem { Text = column.Name });

        // var listStackLayout = new StackLayout
        // {
        //     Items =
        //     {
        //         list
        //     }
        // };

        Content = new StackLayout
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Spacing = 5,
            Padding = 10,
            Items =
            {
                list,
                new StackLayoutItem(grid, true)
            }
        };

        CreateMenuToolBar();
    }

    private void CreateMenuToolBar()
    {
        if (Platform.Supports<MenuBar>())
        {
            var fileCommand = new Command
                { MenuText = "File Command", Shortcut = Application.Instance.CommonModifier | Keys.F };

            var file = new SubMenuItem { Text = "&File", Items = { fileCommand } };

            Menu = new MenuBar
            {
                Items = { file }
            };
        }
    }
}