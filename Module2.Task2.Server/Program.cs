using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task2.Server;

internal static class Program
{
    private const int Port = 5003;

    public static void Main(string[] args)
    {
        var listenEndPoint = new IPEndPoint(IPAddress.Any, Port);

        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(listenEndPoint);
        listener.Listen(backlog: 10);

        Console.WriteLine($"Server listening on {listenEndPoint} (TCP).");
        Console.WriteLine("Requests: DATE or TIME. Connection closes after response.");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        while (true)
        {
            using Socket client = listener.Accept();
            var remote = client.RemoteEndPoint?.ToString() ?? "unknown";

            var request = ReceiveUtf8(client).Trim();
            var response = BuildResponse(request);

            Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} запрос: {request}");
            Console.WriteLine($"Ответ: {response}\n");

            SendUtf8(client, response);
            try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
        }
    }

    private static string BuildResponse(string request)
    {
        var now = DateTime.Now;
        return request.ToUpperInvariant() switch
        {
            "DATE" => now.ToString("yyyy-MM-dd"),
            "TIME" => now.ToString("HH:mm:ss"),
            _ => "ERROR: send DATE or TIME",
        };
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
