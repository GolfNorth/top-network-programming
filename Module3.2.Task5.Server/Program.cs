using Module3_2.Task5.Server;

internal static class Program
{
    public static async Task Main()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "server.log");
        using var logger = new FileLogger(logPath);

        var server = new Server(7005, logger);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await server.RunAsync(cts.Token);
    }
}
