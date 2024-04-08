using System.Diagnostics;

namespace SlLib.Workspace;

public sealed class ContextualTimer : IDisposable
{
    private readonly Stopwatch _timer;
    private readonly string _name;

    public ContextualTimer(string name)
    {
        _name = name;
        _timer = new Stopwatch();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        Console.WriteLine($@"[{_name}] {_timer.Elapsed:m\:ss\.fff}");
    }
}