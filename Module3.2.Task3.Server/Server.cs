using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_2.Common;

namespace Module3_2.Task3.Server;

public sealed class Server(int port)
{
    private const int MaxClients = 10;
    private static readonly TimeSpan InactiveTimeout = TimeSpan.FromMinutes(10);

    private readonly Dictionary<string, DateTime> _clients = new(); // IP -> lastSeen
    private readonly Lock _lock = new();

    public event Action<string>? Log;

    public async Task RunAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(port);
        Log?.Invoke($"Server started on port {port}. Max clients: {MaxClients}, timeout: 10 min.");

        _ = CleanupLoopAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(ct);
            _ = HandleAsync(udp, result);
        }
    }

    private async Task CleanupLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
            var now = DateTime.Now;
            lock (_lock)
            {
                var expired = _clients
                    .Where(kv => now - kv.Value > InactiveTimeout)
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var ip in expired)
                {
                    _clients.Remove(ip);
                    Log?.Invoke($"Client {ip} timed out (inactive > 10 min)");
                }
            }
        }
    }

    private async Task HandleAsync(UdpClient udp, UdpReceiveResult result)
    {
        var request = Encoding.UTF8.GetString(result.Buffer).Trim();
        var ip = result.RemoteEndPoint.Address.ToString();

        string response;
        lock (_lock)
        {
            var isKnown = _clients.ContainsKey(ip);
            if (!isKnown && _clients.Count >= MaxClients)
            {
                response = "SERVER_FULL: max clients reached";
                Log?.Invoke($"{result.RemoteEndPoint}: rejected (server full)");
                _ = SendAsync(udp, response, result.RemoteEndPoint);
                return;
            }
            _clients[ip] = DateTime.Now;
            if (!isKnown) Log?.Invoke($"{result.RemoteEndPoint}: new client (active: {_clients.Count}/{MaxClients})");
        }

        if (request.Equals("BYE", StringComparison.OrdinalIgnoreCase))
        {
            lock (_lock) _clients.Remove(ip);
            response = "BYE";
            Log?.Invoke($"{result.RemoteEndPoint}: disconnected");
        }
        else
        {
            response = Process(request);
            Log?.Invoke($"{result.RemoteEndPoint}: {request} -> {response}");
        }

        await SendAsync(udp, response, result.RemoteEndPoint);
    }

    private static Task SendAsync(UdpClient udp, string message, IPEndPoint ep)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        return udp.SendAsync(bytes, ep).AsTask();
    }

    private static string Process(string request)
    {
        if (request.Equals("LIST", StringComparison.OrdinalIgnoreCase))
            return string.Join("\n", PartsRepository.Parts.Select(kv => $"{kv.Key}: {kv.Value:N0} rub"));

        if (request.StartsWith("PRICE ", StringComparison.OrdinalIgnoreCase))
        {
            var part = request[6..].Trim();
            var price = PartsRepository.GetPrice(part);
            return price.HasValue ? $"{part}: {price.Value:N0} rub" : $"NOT_FOUND: {part}";
        }

        return "ERROR: use LIST, PRICE <name> or BYE";
    }
}
