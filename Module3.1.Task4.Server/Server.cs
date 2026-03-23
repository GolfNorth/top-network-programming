using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_1.Common;

namespace Module3_1.Task4.Server;

public sealed class Server(int port)
{
    private static readonly Dictionary<string, string> ValidUsers = new()
    {
        { "alice", "secret123" },
        { "bob", "qwerty" },
        { "admin", "admin" },
    };
    public event Action<string>? Log;

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Log?.Invoke($"Server started on port {port}. Auth required.");
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(tcpClient, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally { listener.Stop(); }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
    {
        using var client = tcpClient;
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        var authLine = await reader.ReadLineAsync(ct);
        var parts = authLine?.Split() ?? [];

        if (parts.Length < 3
            || !parts[0].Equals("AUTH", StringComparison.OrdinalIgnoreCase)
            || !ValidUsers.TryGetValue(parts[1], out var expectedPassword)
            || expectedPassword != parts[2])
        {
            await writer.WriteLineAsync("AUTH_FAIL");
            Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} auth failed");
            return;
        }

        var username = parts[1];
        await writer.WriteLineAsync("AUTH_OK");
        var quotesSent = new List<string>();
        Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} connected as {username}");

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (line.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("BYE");
                    break;
                }
                if (line.Equals("QUOTE", StringComparison.OrdinalIgnoreCase))
                {
                    var quote = QuoteRepository.GetRandom();
                    quotesSent.Add(quote);
                    await writer.WriteLineAsync(quote);
                    Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} ({username}) -> {quote}");
                }
                else { await writer.WriteLineAsync("ERROR"); }
            }
        }
        catch (Exception) { }

        Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} ({username}) disconnected. Quotes: {quotesSent.Count}");
    }
}
