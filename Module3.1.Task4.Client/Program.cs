using Module3_1.Task4.Client;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : "127.0.0.1";

        Console.Write("Логин: ");
        var username = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Пароль: ");
        var password = Console.ReadLine()?.Trim() ?? string.Empty;

        using var client = new Client(host, 6004);

        try
        {
            await client.ConnectAsync(username, password);
        }
        catch (AuthFailedException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine("Аутентификация успешна. Enter — получить цитату, 'q' — выйти.");

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                break;

            var quote = await client.GetQuoteAsync();
            if (string.IsNullOrEmpty(quote))
            {
                Console.WriteLine("Соединение разорвано сервером.");
                return;
            }

            Console.WriteLine($"\n{quote}\n");
        }

        await client.DisconnectAsync();
    }
}
