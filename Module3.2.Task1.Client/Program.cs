using Module3_2.Task1.Client;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        using var client = new Client(host, 7001);

        Console.WriteLine("Commands: LIST, PRICE <name>, quit");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;
            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            var response = await client.SendAsync(input);
            Console.WriteLine(response);
        }
    }
}
