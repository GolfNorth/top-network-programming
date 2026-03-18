using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task1.Client;

internal static class Program
{
    private const int Port = 5002;

    public static void Main(string[] args)
    {
        var serverIp = args.Length > 0 ? args[0] : "127.0.0.1";
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), Port);

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(serverEndPoint);

        SendUtf8(client, "Привет, сервер!");

        var reply = ReceiveUtf8(client);
        var remote = client.RemoteEndPoint?.ToString() ?? serverEndPoint.ToString();
        Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {reply}");

        try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
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
