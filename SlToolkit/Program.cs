using Eto.Forms;
using SlToolkit.UI;

namespace SlToolkit;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var app = new Application();
        app.UnhandledException += (sender, eventArgs) => { DisplayExceptionMessage(eventArgs.ExceptionObject); };

        try
        {
            app.Run(new MainForm());
        }
        catch (Exception ex)
        {
            DisplayExceptionMessage(ex);
        }

        return;

        void DisplayExceptionMessage(object exception)
        {
            MessageBox.Show($"An unhandled exception occurred!\n\nDetails: {exception}", "Critical Error");
        }
    }
}