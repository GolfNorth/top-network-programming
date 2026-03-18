using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task3.Server;

internal static class Program
{
    private const int Port = 5004;

    public static async Task Main(string[] args)
    {
        var listenEndPoint = new IPEndPoint(IPAddress.Any, Port);

        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(listenEndPoint);
        listener.Listen(backlog: 10);

        Console.WriteLine($"Server listening on {listenEndPoint} (TCP, async).");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        while (true)
        {
            Socket client = await listener.AcceptAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private static async Task HandleClientAsync(Socket client)
    {
        using (client)
        {
            var remote = client.RemoteEndPoint?.ToString() ?? "unknown";

            var received = await ReceiveUtf8Async(client);
            Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {received}");

            var reply = "Привет, клиент!";
            await SendUtf8Async(client, reply);

            try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
        }
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
