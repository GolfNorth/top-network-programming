namespace Module3_2.Task5.Server;

public sealed class FileLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Lock _lock = new();

    public FileLogger(string path)
    {
        _writer = new StreamWriter(path, append: true, System.Text.Encoding.UTF8) { AutoFlush = true };
    }

    public void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        lock (_lock) _writer.WriteLine(line);
        Console.WriteLine(line);
    }

    public void Dispose() => _writer.Dispose();
}
