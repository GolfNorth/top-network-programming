using System.Net.Sockets;
using System.Text;

namespace Module3_2.Task1.Client;

public sealed class Client : IDisposable
{
    private readonly UdpClient _udp;

    public Client(string host, int port)
    {
        _udp = new UdpClient();
        _udp.Connect(host, port);
    }

    public async Task<string> SendAsync(string command, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(command);
        await _udp.SendAsync(bytes, ct);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var result = await _udp.ReceiveAsync(timeout.Token);
        return Encoding.UTF8.GetString(result.Buffer);
    }

    public void Dispose() => _udp.Dispose();
}
