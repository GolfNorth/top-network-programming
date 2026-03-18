using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task4.Client.Legacy;

internal static class Program
{
    private const int Port = 5005;

    private sealed class ClientState
    {
        public Socket Socket { get; init; } = null!;
        public byte[] Buffer { get; } = new byte[4096];
        public string Request { get; init; } = string.Empty;
        public ManualResetEventSlim Done { get; } = new();
    }

    public static void Main(string[] args)
    {
        var serverIp = args.Length > 0 ? args[0] : "127.0.0.1";
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), Port);

        Console.Write("Запросить DATE или TIME: ");
        var request = (Console.ReadLine() ?? string.Empty).Trim();

        var state = new ClientState
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
            Request = request,
        };

        state.Socket.BeginConnect(serverEndPoint, OnConnect, state);
        state.Done.Wait();
    }

    private static void OnConnect(IAsyncResult ar)
    {
        var state = (ClientState)ar.AsyncState!;
        state.Socket.EndConnect(ar);

        var message = Encoding.UTF8.GetBytes(state.Request);
        state.Socket.BeginSend(message, 0, message.Length, SocketFlags.None, OnSend, state);
    }

    private static void OnSend(IAsyncResult ar)
    {
        var state = (ClientState)ar.AsyncState!;
        state.Socket.EndSend(ar);

        state.Socket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnReceive, state);
    }

    private static void OnReceive(IAsyncResult ar)
    {
        var state = (ClientState)ar.AsyncState!;
        var count = state.Socket.EndReceive(ar);

        var reply = Encoding.UTF8.GetString(state.Buffer, 0, count);
        var remote = state.Socket.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {reply}");

        try { state.Socket.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
        state.Socket.Dispose();
        state.Done.Set();
    }
}
