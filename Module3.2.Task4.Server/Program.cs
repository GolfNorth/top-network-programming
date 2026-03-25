namespace Module3_2.Task4.Server;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ServerForm());
    }
}
