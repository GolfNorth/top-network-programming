using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task4.Client;

internal static class Program
{
    private const int Port = 5005;

    public static async Task Main(string[] args)
    {
        var serverIp = args.Length > 0 ? args[0] : "127.0.0.1";
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), Port);

        Console.Write("Запросить DATE или TIME: ");
        var request = (Console.ReadLine() ?? string.Empty).Trim();

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(serverEndPoint);

        await SendUtf8Async(client, request);

        var reply = await ReceiveUtf8Async(client);
        var remote = client.RemoteEndPoint?.ToString() ?? serverEndPoint.ToString();
        Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {reply}");

        try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
    }

    private static async Task<string> ReceiveUtf8Async(Socket socket)
    {
        var buffer = new byte[4096];
        var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    private static async Task SendUtf8Async(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(bytes, SocketFlags.None);
    }
}
