using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_2.Common;

namespace Module3_2.Task5.Server;

public sealed class Server(int port, FileLogger logger)
{
    private const int MaxClients = 10;
    private const int MaxRequestsPerHour = 10;
    private static readonly TimeSpan InactiveTimeout = TimeSpan.FromMinutes(10);

    private readonly Dictionary<string, DateTime> _clients = new();
    private readonly Dictionary<string, (int Count, DateTime WindowStart)> _rateMap = new();
    private readonly Lock _lock = new();

    public async Task RunAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(port);
        logger.Log($"Server started on port {port}. MaxClients={MaxClients}, RateLimit={MaxRequestsPerHour}/hr.");

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
                var expired = _clients.Where(kv => now - kv.Value > InactiveTimeout)
                                      .Select(kv => kv.Key).ToList();
                foreach (var ip in expired)
                {
                    _clients.Remove(ip);
                    logger.Log($"Client {ip} removed (inactivity timeout)");
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
                logger.Log($"{ip}: rejected — server full");
                _ = SendAsync(udp, "SERVER_FULL", result.RemoteEndPoint);
                return;
            }

            if (!CheckRateLimit(ip))
            {
                logger.Log($"{ip}: rate limited");
                _ = SendAsync(udp, "RATE_LIMIT: max 10 requests per hour", result.RemoteEndPoint);
                return;
            }

            if (!isKnown)
                logger.Log($"{ip}: new client connected (active: {_clients.Count + 1}/{MaxClients})");

            _clients[ip] = DateTime.Now;
        }

        if (request.Equals("BYE", StringComparison.OrdinalIgnoreCase))
        {
            lock (_lock) _clients.Remove(ip);
            logger.Log($"{ip}: disconnected via BYE");
            response = "BYE";
        }
        else
        {
            response = Process(request);
            logger.Log($"{ip}: [{request}] -> [{response}]");
        }

        await SendAsync(udp, response, result.RemoteEndPoint);
    }

    private bool CheckRateLimit(string ip)
    {
        var now = DateTime.Now;
        if (_rateMap.TryGetValue(ip, out var entry))
        {
            if (now - entry.WindowStart >= TimeSpan.FromHours(1))
                _rateMap[ip] = (1, now);
            else if (entry.Count >= MaxRequestsPerHour)
                return false;
            else
                _rateMap[ip] = (entry.Count + 1, entry.WindowStart);
        }
        else _rateMap[ip] = (1, now);
        return true;
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

    private static Task SendAsync(UdpClient udp, string message, IPEndPoint ep)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        return udp.SendAsync(bytes, ep).AsTask();
    }
}
