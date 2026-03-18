using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module2.Task4.Server.Legacy;

internal static class Program
{
    private const int Port = 5005;

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
        Console.WriteLine("Requests: DATE or TIME. Connection closes after response.");
        Console.WriteLine("Press Ctrl+C to stop.\n");

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
        var request = Encoding.UTF8.GetString(state.Buffer, 0, count).Trim();
        var response = BuildResponse(request);

        Console.WriteLine($"В {DateTime.Now:HH:mm} от {remote} запрос: {request}");
        Console.WriteLine($"Ответ: {response}\n");

        var reply = Encoding.UTF8.GetBytes(response);
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
}
