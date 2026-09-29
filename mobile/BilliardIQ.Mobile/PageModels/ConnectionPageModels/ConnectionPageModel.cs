using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.ConnectionPageModels;

public partial class ConnectionPageModel : BasePageModel, IDisposable
{
    private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(15);

    private readonly IRaspberryPiConnectionService _connection;
    private CancellationTokenSource? _operationCts;
    private bool _userCancelled;

    public ConnectionPageModel(IRaspberryPiConnectionService connection)
    {
        _connection = connection;
        _connection.StateChanged += OnConnectionStateChanged;
    }

    [ObservableProperty]
    public partial string WebSocketUrl { get; set; } = "ws://192.168.68.19:5288/ws";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConnectionHint))]
    public partial string? ConnectionHint { get; set; }

    public bool HasConnectionHint => !string.IsNullOrEmpty(ConnectionHint);

    public PiConnectionState State => _connection.State;
    public bool IsConnected => State == PiConnectionState.Connected;
    public bool IsNotConnected => !IsConnected;

    public string StatusText => State switch
    {
        PiConnectionState.Connected => string.Format(L["Connect_StatusConnected"], _connection.ActiveConnectionType),
        PiConnectionState.Connecting => L["Connect_StatusConnecting"],
        PiConnectionState.Failed => string.Format(L["Connect_StatusFailed"], _connection.LastErrorMessage),
        _ => L["Connect_StatusDisconnected"]
    };

    [RelayCommand]
    private async Task ConnectWebSocket()
    {
        if (string.IsNullOrWhiteSpace(WebSocketUrl) || IsBusy) return;
        IsBusy = true;
        _userCancelled = false;
        ConnectionHint = null;
        var cts = NewOperationCts(_connectTimeout);
        try
        {
            var connected = await _connection.ConnectViaWebSocketAsync(WebSocketUrl.Trim(), cts.Token);
            if (connected)
                await Shell.Current.GoToAsync("//scoreboard");
            else if (!_userCancelled && cts.IsCancellationRequested)
                ConnectionHint = L["Connect_Timeout"];
        }
        finally
        {
            IsBusy = false;
            ClearOperationCts(cts);
        }
    }

    [RelayCommand]
    private async Task Disconnect() => await _connection.DisconnectAsync();

    [RelayCommand]
    private async Task CancelConnect()
    {
        _userCancelled = true;
        ConnectionHint = null;
        _operationCts?.Cancel();
        await _connection.DisconnectAsync();
        IsBusy = false;
    }

    private CancellationTokenSource NewOperationCts(TimeSpan? timeout = null)
    {
        _operationCts?.Cancel();
        var cts = new CancellationTokenSource();
        if (timeout is { } t)
            cts.CancelAfter(t);
        _operationCts = cts;
        return cts;
    }

    private void ClearOperationCts(CancellationTokenSource cts)
    {
        if (_operationCts == cts)
            _operationCts = null;
        cts.Dispose();
    }

    private void OnConnectionStateChanged(object? sender, PiConnectionState state) => MainThread.BeginInvokeOnMainThread(() =>
                                                                                           {
                                                                                               OnPropertyChanged(nameof(State));
                                                                                               OnPropertyChanged(nameof(IsConnected));
                                                                                               OnPropertyChanged(nameof(IsNotConnected));
                                                                                               OnPropertyChanged(nameof(StatusText));
                                                                                           });

    public void Dispose()
    {
        _connection.StateChanged -= OnConnectionStateChanged;
        GC.SuppressFinalize(this);
    }
}
