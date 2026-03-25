namespace Module3_2.Task4.Client;

public class ClientForm : Form
{
    private PriceClient? _client;

    private readonly TextBox _txtHost = new() { Text = "127.0.0.1", Left = 10, Top = 10, Width = 150 };
    private readonly Button _btnConnect = new() { Text = "Connect", Left = 170, Top = 8, Width = 90 };
    private readonly Button _btnDisconnect = new() { Text = "Disconnect", Left = 270, Top = 8, Width = 100, Enabled = false };
    private readonly TextBox _txtPart = new() { Left = 10, Top = 50, Width = 250, PlaceholderText = "Part name (e.g. CPU)" };
    private readonly Button _btnPrice = new() { Text = "Get Price", Left = 270, Top = 48, Width = 90, Enabled = false };
    private readonly Button _btnList = new() { Text = "List All", Left = 370, Top = 48, Width = 90, Enabled = false };
    private readonly ListBox _lstResults = new() { Left = 10, Top = 90, Width = 460, Height = 310 };

    public ClientForm()
    {
        Text = "UDP Price Client";
        Width = 500; Height = 450;

        _btnConnect.Click += OnConnect;
        _btnDisconnect.Click += async (_, _) => await OnDisconnectAsync();
        _btnPrice.Click += async (_, _) => await OnPriceAsync();
        _btnList.Click += async (_, _) => await OnListAsync();

        Controls.AddRange([_txtHost, _btnConnect, _btnDisconnect,
            _txtPart, _btnPrice, _btnList, _lstResults]);
    }

    private void OnConnect(object? s, EventArgs e)
    {
        _client = new PriceClient(_txtHost.Text.Trim(), 7004);
        _btnConnect.Enabled = false;
        _btnDisconnect.Enabled = _btnPrice.Enabled = _btnList.Enabled = true;
        AddLog("Connected.");
    }

    private async Task OnDisconnectAsync()
    {
        await TrySendAsync("BYE");
        _client?.Dispose();
        _client = null;
        _btnConnect.Enabled = true;
        _btnDisconnect.Enabled = _btnPrice.Enabled = _btnList.Enabled = false;
        AddLog("Disconnected.");
    }

    private async Task OnPriceAsync()
    {
        var part = _txtPart.Text.Trim();
        if (!string.IsNullOrEmpty(part))
            AddLog(await TrySendAsync($"PRICE {part}") ?? "No response");
    }

    private async Task OnListAsync()
    {
        var response = await TrySendAsync("LIST");
        if (response is not null)
            foreach (var line in response.Split(new[] { (char)10 }, StringSplitOptions.RemoveEmptyEntries))
                AddLog(line);
    }

    private async Task<string?> TrySendAsync(string command)
    {
        if (_client is null) return null;
        try { return await _client.SendAsync(command); }
        catch (Exception ex) { AddLog($"Error: {ex.Message}"); return null; }
    }

    private void AddLog(string msg) => _lstResults.Items.Add(msg);
}
