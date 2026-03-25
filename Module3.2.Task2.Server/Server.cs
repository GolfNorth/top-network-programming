using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_2.Common;

namespace Module3_2.Task2.Server;

public sealed class Server(int port)
{
    private const int MaxRequestsPerHour = 10;

    private readonly Dictionary<string, (int Count, DateTime WindowStart)> _rateMap = new();
    private readonly Lock _lock = new();

    public event Action<string>? Log;

    public async Task RunAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(port);
        Log?.Invoke($"Server started on port {port}. Limit: {MaxRequestsPerHour} req/hour per client.");

        while (!ct.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(ct);
            _ = HandleAsync(udp, result);
        }
    }

    private async Task HandleAsync(UdpClient udp, UdpReceiveResult result)
    {
        var request = Encoding.UTF8.GetString(result.Buffer).Trim();
        var ip = result.RemoteEndPoint.Address.ToString();

        string response;
        if (!CheckRateLimit(ip))
        {
            response = "RATE_LIMIT: max 10 requests per hour";
            Log?.Invoke($"{result.RemoteEndPoint}: RATE_LIMITED");
        }
        else
        {
            response = Process(request);
            Log?.Invoke($"{result.RemoteEndPoint}: {request} -> {response}");
        }

        var bytes = Encoding.UTF8.GetBytes(response);
        await udp.SendAsync(bytes, result.RemoteEndPoint);
    }

    private bool CheckRateLimit(string ip)
    {
        lock (_lock)
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
            else
            {
                _rateMap[ip] = (1, now);
            }
            return true;
        }
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

        return "ERROR: use LIST or PRICE <name>";
    }
}
