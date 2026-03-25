using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module3_3.Task2.Server;

class Program
{
    const int Port = 6002;

    static async Task Main()
    {
        var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Any, Port));
        server.Listen(128);

        Console.WriteLine($"Task2 Tic-Tac-Toe Server on port {Port}");

        while (true)
        {
            Console.WriteLine("Waiting for player 1...");
            var p1 = await server.AcceptAsync();
            await SendLineAsync(p1, "Waiting for second player...");

            Console.WriteLine("Waiting for player 2...");
            var p2 = await server.AcceptAsync();

            Console.WriteLine("Game started!");
            _ = PlayGameAsync(p1, p2);
        }
    }

    static async Task PlayGameAsync(Socket p1, Socket p2)
    {
        await SendLineAsync(p1, "You are X. Game starts!");
        await SendLineAsync(p2, "You are O. Game starts!");

        var board = new char[9];
        Array.Fill(board, '.');
        char current = 'X';

        while (true)
        {
            var curSock   = current == 'X' ? p1 : p2;
            var otherSock = current == 'X' ? p2 : p1;

            await SendLineAsync(curSock,   BoardStr(board) + $"Your turn ({current}), enter cell (1-9):");
            await SendLineAsync(otherSock, BoardStr(board) + "Waiting for opponent...");

            string? input = await ReceiveLineAsync(curSock);
            if (input is null)
            {
                await SendLineAsync(otherSock, "Opponent disconnected. You win!");
                break;
            }

            if (!int.TryParse(input, out int cell) || cell < 1 || cell > 9 || board[cell - 1] != '.')
            {
                await SendLineAsync(curSock, "Invalid move, try again.");
                continue;
            }

            board[cell - 1] = current;
            char winner = CheckWinner(board);

            if (winner != '\0')
            {
                string finalBoard = BoardStr(board);
                if (winner == 'D')
                {
                    await SendLineAsync(p1, finalBoard + "Draw!");
                    await SendLineAsync(p2, finalBoard + "Draw!");
                }
                else
                {
                    var w = winner == 'X' ? p1 : p2;
                    var l = winner == 'X' ? p2 : p1;
                    await SendLineAsync(w, finalBoard + "You win!");
                    await SendLineAsync(l, finalBoard + "You lose.");
                }
                break;
            }

            current = current == 'X' ? 'O' : 'X';
        }

        p1.Dispose();
        p2.Dispose();
    }

    static string BoardStr(char[] b) =>
        $"\n {b[0]} | {b[1]} | {b[2]}\n---+---+---\n {b[3]} | {b[4]} | {b[5]}\n---+---+---\n {b[6]} | {b[7]} | {b[8]}\n";

    static char CheckWinner(char[] b)
    {
        int[][] lines = [[0,1,2],[3,4,5],[6,7,8],[0,3,6],[1,4,7],[2,5,8],[0,4,8],[2,4,6]];
        foreach (var l in lines)
            if (b[l[0]] != '.' && b[l[0]] == b[l[1]] && b[l[1]] == b[l[2]])
                return b[l[0]];
        return Array.Exists(b, c => c == '.') ? '\0' : 'D';
    }

    static Task SendLineAsync(Socket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        return socket.SendAsync(bytes, SocketFlags.None);
    }

    static async Task<string?> ReceiveLineAsync(Socket socket)
    {
        var buffer = new byte[4096];
        int count = await socket.ReceiveAsync(buffer, SocketFlags.None);
        return count == 0 ? null : Encoding.UTF8.GetString(buffer, 0, count).Trim();
    }
}
