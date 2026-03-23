using Module3_1.Task3.Server;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var server = new Server(6003);
        server.Log += Console.WriteLine;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await server.RunAsync(cts.Token);
    }
}
