namespace Module3_2.Task4.Server;

public partial class ServerForm : Form
{
    private readonly UdpServer _server = new(7004);
    private CancellationTokenSource? _cts;

    private Button _btnStart = null!;
    private Button _btnStop = null!;
    private Label _lblClients = null!;
    private ListBox _lstLog = null!;

    public ServerForm()
    {
        Text = "UDP Price Server";
        Width = 600; Height = 450;
        FormClosing += (_, _) => _cts?.Cancel();

        _btnStart = new Button { Text = "Start", Left = 10, Top = 10, Width = 80 };
        _btnStop = new Button { Text = "Stop", Left = 100, Top = 10, Width = 80, Enabled = false };
        _lblClients = new Label { Text = "Clients: 0", Left = 200, Top = 15, Width = 150 };
        _lstLog = new ListBox { Left = 10, Top = 50, Width = 560, Height = 340, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom };

        _btnStart.Click += OnStart;
        _btnStop.Click += OnStop;

        Controls.AddRange([_btnStart, _btnStop, _lblClients, _lstLog]);

        _server.Log += msg => Invoke(() =>
        {
            _lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            _lstLog.TopIndex = _lstLog.Items.Count - 1;
        });
        _server.ClientCountChanged += count => Invoke(() =>
            _lblClients.Text = $"Clients: {count}");
    }

    private void OnStart(object? s, EventArgs e)
    {
        _cts = new CancellationTokenSource();
        _ = _server.StartAsync(_cts.Token);
        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
    }

    private void OnStop(object? s, EventArgs e)
    {
        _cts?.Cancel();
        _server.Stop();
        _btnStart.Enabled = true;
        _btnStop.Enabled = false;
    }
}
