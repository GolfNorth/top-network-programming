using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module3_3.Task1.Server;

class Program
{
    const int Port = 6001;

    static async Task Main()
    {
        var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Any, Port));
        server.Listen(128);

        Console.WriteLine($"Task1 Server listening on port {Port}");

        while (true)
        {
            var client = await server.AcceptAsync();
            _ = HandleClientAsync(client);
        }
    }

    static async Task HandleClientAsync(Socket client)
    {
        var ep = client.RemoteEndPoint;
        Console.WriteLine($"[+] {ep} connected");
        try
        {
            var buffer = new byte[4096];
            while (true)
            {
                int count = await client.ReceiveAsync(buffer, SocketFlags.None);
                if (count == 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, count).Trim();

                if (msg.Equals("BYE", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[{ep}] disconnected");
                    await SendLineAsync(client, "BYE");
                    break;
                }

                Console.WriteLine($"[{ep}] {msg}");
                await SendLineAsync(client, $"ECHO: {msg}");
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    static Task SendLineAsync(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        return socket.SendAsync(bytes, SocketFlags.None);
    }
}
