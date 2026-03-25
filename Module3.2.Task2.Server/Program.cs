using Module3_2.Task2.Server;

internal static class Program
{
    public static async Task Main()
    {
        var server = new Server(7002);
        server.Log += Console.WriteLine;
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await server.RunAsync(cts.Token);
    }
}
