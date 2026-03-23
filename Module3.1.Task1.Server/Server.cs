using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_1.Common;

namespace Module3_1.Task1.Server;

public sealed class Server(int port)
{
    public event Action<string>? Log;

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Log?.Invoke($"Server started on port {port}.");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(tcpClient, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
    {
        using var client = tcpClient;
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var quotesSent = new List<string>();
        Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} connected");

        try
        {
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

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
                    Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} -> {quote}");
                }
                else
                {
                    await writer.WriteLineAsync("ERROR");
                }
            }
        }
        catch (Exception) { }

        Log?.Invoke($"{DateTime.Now:HH:mm:ss} {remote} disconnected. Quotes: {quotesSent.Count}");
    }
}
