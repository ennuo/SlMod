using System.Collections.ObjectModel;
using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using SlLib.Lookup;
using SlLib.Resources.Excel;
using SlLib.Utilities;
using Cell = SlLib.Resources.Excel.Cell;

namespace SlToolkit.UI;

class CellModel : INotifyPropertyChanged
{
    public string Name { get; }
    public Type Type { get; }
    public Cell Cell { get; }
    
    public CellModel(Cell cell)
    {
        Cell = cell;
        Name = ExcelPropertyNameLookup.GetPropertyName(cell.Name);
        
        if (cell.Type == CellType.Float) Type = typeof(float);
        else if (cell.Type == CellType.Int) Type = typeof(int);
        else if (cell.Type == CellType.String) Type = typeof(string);
        else if (cell.Type == CellType.Uint) Type = typeof(uint);
    }
    
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

        var filtered = data.GetWorksheet("Racers")!.GetColumnByName("dragon")!.Cells.Select(cell => new CellModel(cell)).ToList();

        var valueColumn = new GridColumn
        {
            AutoSize = true,
            Editable = true,
            HeaderText = "Value",
            DataCell = propertyCell
        };

        var grid = new GridView { Columns = { infoColumn, valueColumn } };
        grid.DataStore = filtered;

        Content = new StackLayout
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Spacing = 5,
            Padding = 10,
            Items =
            {
                new StackLayoutItem(grid, expand: true)
            }
        };
        
        // var cells = data.GetWorksheet("Racers")!.GetColumnByName("dragon")!.Cells;
        //
        //
        // var grid = new GridView { DataStore = cells };
        // grid.Columns.Add(new GridColumn
        // {
        //     DataCell = new TextBoxCell { Binding = Binding.Property<Cell, string>(r => r.Name + "") },
        //     HeaderText = "Name"
        // });
        //
        // grid.Columns.Add(new GridColumn
        // {
        //     Editable = true,
        //     DataCell = new TextBoxCell { Binding = Binding.Property<Cell, string>(r => r.Value.ToString()!) },
        //     HeaderText = "Value"
        // });
        //
        // var scroll = new Scrollable();
        // scroll.Content = grid;
        //
        // Content = new TableLayout
        // {
        //     Spacing = new Size(5, 5),
        //     Padding = new Padding(10, 10, 10, 10),
        //     Rows =
        //     {
        //         new TableRow(
        //             new TableCell(new Label { Text = "Racers" }, true),
        //             new TableCell(new Label { Text = "Data" }, true)
        //         ),
        //         new TableRow(
        //             new TextBox { Text = "Some text" },
        //             scroll
        //         ),
        //         // by default, the last row & column will get scaled. This adds a row at the end to take the extra space of the form.
        //         // otherwise, the above row will get scaled and stretch the TextBox/ComboBox/CheckBox to fill the remaining height.
        //         new TableRow { ScaleHeight = true }
        //     }
        // };
        
        CreateMenuToolBar();
    }

    private void CreateMenuToolBar()
    {
        if (Platform.Supports<MenuBar>())
        {
            var fileCommand = new Command
                { MenuText = "File Command", Shortcut = Application.Instance.CommonModifier | Keys.F };

            var file = new SubMenuItem() { Text = "&File", Items = { fileCommand } };
            
            Menu = new MenuBar
            {
                Items = { file }
            };
        }
    }
}