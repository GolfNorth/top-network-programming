using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task1.Server;

internal static class Program
{
    private const int Port = 5002;

    public static void Main(string[] args)
    {
        var listenEndPoint = new IPEndPoint(IPAddress.Any, Port);

        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(listenEndPoint);
        listener.Listen(backlog: 10);

        Console.WriteLine($"Server listening on {listenEndPoint} (TCP).");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        while (true)
        {
            using Socket client = listener.Accept();
            var remote = client.RemoteEndPoint?.ToString() ?? "unknown";

            var received = ReceiveUtf8(client);
            Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {received}");

            var reply = "Привет, клиент!";
            SendUtf8(client, reply);
            try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
        }
    }

    private static string ReceiveUtf8(Socket socket)
    {
        var buffer = new byte[4096];
        var received = socket.Receive(buffer);
        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    private static void SendUtf8(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        socket.Send(bytes);
    }
}
