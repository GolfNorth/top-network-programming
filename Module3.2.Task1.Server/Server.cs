using System.Net;
using System.Net.Sockets;
using System.Text;
using Module3_2.Common;

namespace Module3_2.Task1.Server;

public sealed class Server(int port)
{
    public event Action<string>? Log;

    public async Task RunAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(port);
        Log?.Invoke($"Server started on port {port}. Commands: LIST, PRICE <name>");

        while (!ct.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(ct);
            _ = HandleAsync(udp, result);
        }
    }

    private async Task HandleAsync(UdpClient udp, UdpReceiveResult result)
    {
        var request = Encoding.UTF8.GetString(result.Buffer).Trim();
        var response = Process(request);
        var bytes = Encoding.UTF8.GetBytes(response);
        await udp.SendAsync(bytes, result.RemoteEndPoint);
        Log?.Invoke($"{result.RemoteEndPoint}: {request} -> {response}");
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
