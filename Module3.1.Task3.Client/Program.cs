using Module3_1.Task3.Client;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        using var client = new Client(host, 6003);

        try
        {
            await client.ConnectAsync();
        }
        catch (ServerFullException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine("Подключено. Enter — получить цитату, 'q' — выйти.");

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
