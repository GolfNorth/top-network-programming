using System.Net.Sockets;
using System.Text;

namespace Module3_1.Task4.Client;

public sealed class AuthFailedException(string message) : Exception(message);

public sealed class Client(string host, int port) : IDisposable
{
    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task ConnectAsync(string username, string password)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(host, port);
        var stream = _tcp.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        await _writer.WriteLineAsync($"AUTH {username} {password}");
        var response = await _reader.ReadLineAsync();
        if (response != "AUTH_OK")
            throw new AuthFailedException("Wrong login or password.");
    }

    public async Task<string> GetQuoteAsync()
    {
        await _writer!.WriteLineAsync("QUOTE");
        return await _reader!.ReadLineAsync() ?? string.Empty;
    }

    public async Task DisconnectAsync()
    {
        await _writer!.WriteLineAsync("BYE");
        await _reader!.ReadLineAsync();
    }

    public void Dispose() => _tcp?.Dispose();
}
