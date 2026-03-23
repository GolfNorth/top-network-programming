using System.Net.Sockets;
using System.Text;

namespace Module3_1.Common;

public static class NetworkStreamHelper
{
    public static async Task<string?> ReceiveUtf8Async(this NetworkStream stream, CancellationToken ct = default)
    {
        var buffer = new byte[1024];
        var count = await stream.ReadAsync(buffer, ct);
        return count == 0 ? null : Encoding.UTF8.GetString(buffer, 0, count).Trim();
    }

    public static async Task SendUtf8Async(this NetworkStream stream, string message, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(bytes, ct);
    }
}
