using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module3_3.Task2.Client;

class Program
{
    const int Port = 6002;

    static async Task Main(string[] args)
    {
        string host = args.Length > 0 ? args[0] : "127.0.0.1";

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(host), Port));

        Console.WriteLine($"Connected to {host}:{Port}");

        using var cts = new CancellationTokenSource();

        // Receive loop in background
        var recvTask = ReceiveLoopAsync(socket, cts.Token);

        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            await SendLineAsync(socket, line);
        }

        cts.Cancel();
        await recvTask;
        socket.Dispose();
    }

    static async Task ReceiveLoopAsync(Socket socket, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var leftover = string.Empty;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int count = await socket.ReceiveAsync(buffer, SocketFlags.None, ct);
                if (count == 0) break;

                leftover += Encoding.UTF8.GetString(buffer, 0, count);
                int pos;
                while ((pos = leftover.IndexOf('\n')) >= 0)
                {
                    Console.WriteLine(leftover[..pos].TrimEnd('\r'));
                    leftover = leftover[(pos + 1)..];
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
    }

    static Task SendLineAsync(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        return socket.SendAsync(bytes, SocketFlags.None);
    }
}
