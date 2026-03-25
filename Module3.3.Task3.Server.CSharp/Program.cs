using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Module3_3.Task3.Server;

enum PlayerType { Human, Computer }

class Program
{
    const int Port = 6003;
    static readonly Random Rng = new();

    static async Task Main()
    {
        var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(new IPEndPoint(IPAddress.Any, Port));
        server.Listen(128);

        Console.WriteLine($"Task3 Server on port {Port} (game modes)");

        while (true)
        {
            Console.WriteLine("Waiting for player 1...");
            var p1 = await server.AcceptAsync();

            await SendLineAsync(p1, "Select mode:\n1 - Human vs Human\n2 - Human vs Computer\n3 - Computer vs Computer\nEnter 1/2/3:");
            string? modeStr = await ReceiveLineAsync(p1);

            switch (modeStr)
            {
                case "1":
                    Console.WriteLine("Mode HH: waiting for player 2...");
                    await SendLineAsync(p1, "Waiting for second player...");
                    var p2 = await server.AcceptAsync();
                    _ = PlaySessionAsync(p1, p2, PlayerType.Human, PlayerType.Human);
                    break;
                case "2":
                    Console.WriteLine("Mode HC: starting...");
                    _ = PlaySessionAsync(p1, null, PlayerType.Human, PlayerType.Computer);
                    break;
                case "3":
                    Console.WriteLine("Mode CC: starting...");
                    _ = PlaySessionAsync(p1, null, PlayerType.Computer, PlayerType.Computer);
                    break;
                default:
                    await SendLineAsync(p1, "Invalid mode.");
                    p1.Dispose();
                    break;
            }
        }
    }

    static async Task PlaySessionAsync(Socket p1, Socket? p2, PlayerType t1, PlayerType t2)
    {
        var board = new char[9];
        Array.Fill(board, '.');
        char current = 'X';

        await SendLineAsync(p1, "You are X");
        if (t2 == PlayerType.Human && p2 is not null)
            await SendLineAsync(p2, "You are O");

        while (true)
        {
            var curSock   = current == 'X' ? p1 : p2;
            var otherSock = current == 'X' ? p2 : p1;
            var curType   = current == 'X' ? t1 : t2;

            int cell;

            if (curType == PlayerType.Computer)
            {
                await Task.Delay(500);
                cell = AiMove(board, current);
                await SendLineAsync(p1, BoardStr(board) + $"Computer ({current}) plays cell {cell + 1}");
                if (t2 == PlayerType.Human && p2 is not null)
                    await SendLineAsync(p2, BoardStr(board) + $"Computer ({current}) plays cell {cell + 1}");
            }
            else
            {
                await SendLineAsync(curSock!, BoardStr(board) + $"Your turn ({current}), enter cell (1-9):");
                if (otherSock is not null && (current == 'X' ? t2 : t1) == PlayerType.Human)
                    await SendLineAsync(otherSock, BoardStr(board) + "Waiting for opponent...");

                string? input;
                while (true)
                {
                    input = await ReceiveLineAsync(curSock!);
                    if (input is null)
                    {
                        if (otherSock is not null) await SendLineAsync(otherSock, "Opponent disconnected.");
                        p1.Dispose(); p2?.Dispose();
                        return;
                    }
                    if (int.TryParse(input, out cell) && cell >= 1 && cell <= 9 && board[cell - 1] == '.')
                    {
                        cell--;
                        break;
                    }
                    await SendLineAsync(curSock!, "Invalid move, try again:");
                }
            }

            board[cell] = current;
            char winner = CheckWinner(board);

            if (winner != '\0')
            {
                string finalBoard = BoardStr(board);
                if (winner == 'D')
                {
                    await SendLineAsync(p1, finalBoard + "Draw!");
                    if (t2 == PlayerType.Human && p2 is not null)
                        await SendLineAsync(p2, finalBoard + "Draw!");
                }
                else if (t1 == PlayerType.Human && t2 == PlayerType.Human && p2 is not null)
                {
                    var w = winner == 'X' ? p1 : p2;
                    var l = winner == 'X' ? p2 : p1;
                    await SendLineAsync(w, finalBoard + "You win!");
                    await SendLineAsync(l, finalBoard + "You lose.");
                }
                else if (t1 == PlayerType.Human)
                {
                    bool humanWon = (winner == 'X' && t1 == PlayerType.Human) ||
                                   (winner == 'O' && t2 == PlayerType.Human);
                    await SendLineAsync(p1, finalBoard + (humanWon ? "You win!" : "Computer wins."));
                }
                else
                {
                    await SendLineAsync(p1, finalBoard + $"Computer {winner} wins!");
                }
                break;
            }

            current = current == 'X' ? 'O' : 'X';
        }

        p1.Dispose();
        p2?.Dispose();
    }

    static int AiMove(char[] board, char me)
    {
        int[][] lines = [[0,1,2],[3,4,5],[6,7,8],[0,3,6],[1,4,7],[2,5,8],[0,4,8],[2,4,6]];
        char opp = me == 'X' ? 'O' : 'X';

        // Win
        foreach (var l in lines)
        {
            var empty = l.Where(i => board[i] == '.').ToArray();
            if (empty.Length == 1 && l.Count(i => board[i] == me) == 2) return empty[0];
        }
        // Block
        foreach (var l in lines)
        {
            var empty = l.Where(i => board[i] == '.').ToArray();
            if (empty.Length == 1 && l.Count(i => board[i] == opp) == 2) return empty[0];
        }
        // Center
        if (board[4] == '.') return 4;
        // Random
        var free = Enumerable.Range(0, 9).Where(i => board[i] == '.').ToArray();
        return free[Rng.Next(free.Length)];
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
