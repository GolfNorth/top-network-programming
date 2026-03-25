using System.Net.Sockets;
using System.Text;

namespace Module3_2.Task4.Client;

public sealed class PriceClient : IDisposable
{
    private readonly UdpClient _udp;

    public PriceClient(string host, int port)
    {
        _udp = new UdpClient();
        _udp.Connect(host, port);
    }

    public async Task<string> SendAsync(string command)
    {
        var bytes = Encoding.UTF8.GetBytes(command);
        await _udp.SendAsync(bytes);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await _udp.ReceiveAsync(cts.Token);
        return Encoding.UTF8.GetString(result.Buffer);
    }

    public void Dispose() => _udp.Dispose();
}
