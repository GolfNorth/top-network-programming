using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task3.Server.Legacy;

internal static class Program
{
    private const int Port = 5004;

    // Состояние одного клиентского соединения, передаётся между колбэками
    private sealed class ClientState
    {
        public Socket Socket { get; init; } = null!;
        public byte[] Buffer { get; } = new byte[4096];
    }

    public static void Main(string[] args)
    {
        var listenEndPoint = new IPEndPoint(IPAddress.Any, Port);

        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(listenEndPoint);
        listener.Listen(backlog: 10);

        Console.WriteLine($"Server listening on {listenEndPoint} (TCP, APM Begin/End).");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        // Запускаем первое ожидание входящего соединения
        listener.BeginAccept(OnAccept, listener);

        Console.ReadKey(intercept: true);
    }

    private static void OnAccept(IAsyncResult ar)
    {
        var listener = (Socket)ar.AsyncState!;

        Socket client;
        try
        {
            client = listener.EndAccept(ar);
        }
        catch
        {
            return;
        }

        // Сразу принимаем следующее соединение, не дожидаясь обработки текущего
        listener.BeginAccept(OnAccept, listener);

        var state = new ClientState { Socket = client };
        client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, OnReceive, state);
    }

    private static void OnReceive(IAsyncResult ar)
    {
        var state = (ClientState)ar.AsyncState!;

        int count;
        try
        {
            count = state.Socket.EndReceive(ar);
        }
        catch
        {
            state.Socket.Dispose();
            return;
        }

        var remote = state.Socket.RemoteEndPoint?.ToString() ?? "unknown";
        var received = Encoding.UTF8.GetString(state.Buffer, 0, count);
        Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} получена строка: {received}");

        var reply = Encoding.UTF8.GetBytes("Привет, клиент!");
        state.Socket.BeginSend(reply, 0, reply.Length, SocketFlags.None, OnSend, state.Socket);
    }

    private static void OnSend(IAsyncResult ar)
    {
        var client = (Socket)ar.AsyncState!;
        try
        {
            client.EndSend(ar);
            client.Shutdown(SocketShutdown.Both);
        }
        catch { /* ignore */ }
        finally
        {
            client.Dispose();
        }
    }
}
