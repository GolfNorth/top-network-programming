using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module3_3.Task1.Client;

class Program
{
    const int Port = 6001;

    static async Task Main(string[] args)
    {
        string host = args.Length > 0 ? args[0] : "127.0.0.1";

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), Port));

        Console.WriteLine($"Connected to {host}:{Port}");
        Console.WriteLine("Type messages (BYE to quit):");

        var buffer = new byte[4096];
        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            await SendLineAsync(socket, line);

            int count = await socket.ReceiveAsync(buffer, SocketFlags.None);
            string reply = Encoding.UTF8.GetString(buffer, 0, count).Trim();
            Console.WriteLine($"Server: {reply}");

            if (line.Equals("BYE", StringComparison.OrdinalIgnoreCase)) break;
        }

        socket.Dispose();
    }

    static Task SendLineAsync(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        return socket.SendAsync(bytes, SocketFlags.None);
    }
}
