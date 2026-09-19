using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.PageModels.ScoreboardPageModels;

public partial class ScoreboardPageModel : BasePageModel
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IRaspberryPiConnectionService _connection;

    public ScoreboardPageModel(IRaspberryPiConnectionService connection)
    {
        _connection = connection;
        _connection.MessageReceived += OnMessageReceived;
        _connection.StateChanged += OnConnectionStateChanged;

        Player1Name = L["Game_Player1"];
        Player2Name = L["Game_Player2"];
    }

    public PiConnectionState ConnectionState => _connection.State;

    public string ConnectionStatusText => ConnectionState switch
    {
        PiConnectionState.Connected => string.Format(L["Connect_StatusConnected"], _connection.ActiveConnectionType),
        PiConnectionState.Connecting => L["Connect_StatusConnecting"],
        PiConnectionState.Failed => string.Format(L["Connect_StatusFailed"], _connection.LastErrorMessage),
        _ => L["Connect_StatusDisconnected"]
    };

    public Color ConnectionStatusColor => ConnectionState switch
    {
        PiConnectionState.Connected => Color.FromArgb("#2E7D32"),
        PiConnectionState.Connecting => Color.FromArgb("#F9A825"),
        PiConnectionState.Failed => Color.FromArgb("#C62828"),
        _ => Color.FromArgb("#616161"),
    };

    public bool ShowReconnect => ConnectionState is PiConnectionState.Disconnected or PiConnectionState.Failed;
    public bool CanInteract => ConnectionState == PiConnectionState.Connected;

    [ObservableProperty]
    public partial bool IsReconnecting { get; set; }

    [RelayCommand]
    private async Task Reconnect()
    {
        if (IsReconnecting) return;
        IsReconnecting = true;
        try
        {
            await _connection.ReconnectAsync();
        }
        finally
        {
            IsReconnecting = false;
        }
    }

    [RelayCommand]
    private async Task Appearing()
    {
        var req = new ScoreBoardRequest
        {
            Type = "state",
            Commands = []
        };
        await SendAsync(req);
    }

    private void OnConnectionStateChanged(object? sender, PiConnectionState state) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(ConnectionState));
            OnPropertyChanged(nameof(ConnectionStatusText));
            OnPropertyChanged(nameof(ConnectionStatusColor));
            OnPropertyChanged(nameof(ShowReconnect));
            OnPropertyChanged(nameof(CanInteract));
        });

    private void OnMessageReceived(object? sender, string message)
    {
        ScoreboardStateEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ScoreboardStateEnvelope>(message, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope?.Type != "state" || envelope.State is not { } state) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (state.Player1DisplayName is not null) Player1Name = state.Player1DisplayName;
            if (state.Player2DisplayName is not null) Player2Name = state.Player2DisplayName;
            if (state.Player1Score is { } p1) Player1Score = p1;
            if (state.Player2Score is { } p2) Player2Score = p2;
            if (state.ActivePlayer is { } active) ActivePlayer = active;
            if (state.MatchTarget is { } target) MatchTarget = target;
            if (state.ShotClockActive is { } running) IsTimerRunning = running;
            if (state.Player1Avg is { } avg1) Player1Average = avg1;
            if (state.Player2Avg is { } avg2) Player2Average = avg2;
            if (state.Player1HighRun is { } hr1) Player1HighRun = hr1;
            if (state.Player2HighRun is { } hr2) Player2HighRun = hr2;
            if (state.Inning is { } inning) Inning = inning;
            PendingDelta = 0;
        });
    }
    private sealed class ScoreBoardRequest
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "command";
        [JsonPropertyName("commands")] public ScoreBoardCommand[] Commands { get; set; } = [];
    }
    private sealed class ScoreBoardCommand(string commandName, int? payload = null)
    {
        [JsonPropertyName("command")]
        public string CommandName { get; } = commandName;

        [JsonPropertyName("payload")]
        public int? Payload { get; } = payload;
    }
    private sealed class ScoreboardStateEnvelope
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("state")] public ScoreboardState? State { get; set; }
    }
    private sealed class ScoreboardState
    {
        [JsonPropertyName("player1DisplayName")] public string? Player1DisplayName { get; set; }
        [JsonPropertyName("player2DisplayName")] public string? Player2DisplayName { get; set; }
        [JsonPropertyName("player1Score")] public int? Player1Score { get; set; }
        [JsonPropertyName("player2Score")] public int? Player2Score { get; set; }
        [JsonPropertyName("player1Avg")] public double? Player1Avg { get; set; }
        [JsonPropertyName("player2Avg")] public double? Player2Avg { get; set; }
        [JsonPropertyName("player1HighRun")] public int? Player1HighRun { get; set; }
        [JsonPropertyName("player2HighRun")] public int? Player2HighRun { get; set; }
        [JsonPropertyName("activePlayer")] public int? ActivePlayer { get; set; }
        [JsonPropertyName("inning")] public int? Inning { get; set; }
        [JsonPropertyName("matchTarget")] public int? MatchTarget { get; set; }
        [JsonPropertyName("shotClockActive")] public bool? ShotClockActive { get; set; }
        [JsonPropertyName("currentPoints")] public int? CurrentPoints { get; set; }
        [JsonPropertyName("shotClockSeconds")] public int? ShotClockSeconds { get; set; }
    }

    [ObservableProperty]
    public partial string Player1Name { get; set; }

    [ObservableProperty]
    public partial string Player2Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedPlayer1Score))]
    public partial int Player1Score { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedPlayer2Score))]
    public partial int Player2Score { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedPlayer1Score), nameof(DisplayedPlayer2Score), nameof(HasPendingDelta), nameof(PendingDeltaText), nameof(PendingDeltaColor))]
    public partial int PendingDelta { get; set; }

    [ObservableProperty]
    public partial double Player1Average { get; set; }

    [ObservableProperty]
    public partial double Player2Average { get; set; }

    [ObservableProperty]
    public partial int Player1HighRun { get; set; }

    [ObservableProperty]
    public partial int Player2HighRun { get; set; }

    [ObservableProperty]
    public partial int Inning { get; set; }

    [ObservableProperty]
    public partial int MatchTarget { get; set; } = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayer1Active), nameof(IsPlayer2Active), nameof(Player1StrokeColor), nameof(Player2StrokeColor), nameof(DisplayedPlayer1Score), nameof(DisplayedPlayer2Score))]
    public partial int ActivePlayer { get; set; } = 1;

    public bool IsPlayer1Active => ActivePlayer == 1;
    public bool IsPlayer2Active => ActivePlayer == 2;
    public Color Player1StrokeColor => IsPlayer1Active ? Color.FromArgb("#2E7D32") : Colors.Transparent;
    public Color Player2StrokeColor => IsPlayer2Active ? Color.FromArgb("#C62828") : Colors.Transparent;

    public int DisplayedPlayer1Score => Player1Score + (IsPlayer1Active ? PendingDelta : 0);
    public int DisplayedPlayer2Score => Player2Score + (IsPlayer2Active ? PendingDelta : 0);
    public bool HasPendingDelta => PendingDelta != 0;
    public string PendingDeltaText => PendingDelta.ToString();
    public Color PendingDeltaColor => PendingDelta >= 0 ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimerButtonText))]
    public partial bool IsTimerRunning { get; set; }

    public string TimerButtonText => IsTimerRunning ? L["Scoreboard_Pause"] : L["Scoreboard_Start"];

    private int GetActivePlayerId() => IsPlayer1Active ? 1 : 2;

    [RelayCommand]
    private void IncrementActiveScore() => PendingDelta++;

    [RelayCommand]
    private void DecrementActiveScore()
    {
        var confirmed = IsPlayer1Active ? Player1Score : Player2Score;
        if (confirmed + PendingDelta > 0) PendingDelta--;
    }

    [RelayCommand]
    private async Task SelectPlayer1()
    {
        PendingDelta = 0;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("SelectPlayer1")]
        };
        await SendAsync(req);
        ActivePlayer = 1;
    }

    [RelayCommand]
    private async Task SelectPlayer2()
    {
        PendingDelta = 0;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("SelectPlayer2")]
        };
        await SendAsync(req);
        ActivePlayer = 2;
    }

    [RelayCommand]
    private async Task ConfirmScore()
    {
        if (PendingDelta == 0) return;

        var delta = PendingDelta;
        var req = new ScoreBoardRequest
        {
            Type = "batch",
            Commands = [
                new($"SelectPlayer{GetActivePlayerId()}"),
                new("AdjustPoints", delta),
                new("CommitPoints")
            ]
        };
        await SendAsync(req);
        PendingDelta = 0;
    }

    [RelayCommand]
    private async Task ResetScores()
    {
        Player1Score = 0;
        Player2Score = 0;
        ActivePlayer = 1;
        PendingDelta = 0;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("ResetScore")]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private async Task SendMatchTarget()
    {
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("SetMatchTarget", MatchTarget)]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private async Task ToggleTimer()
    {
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("ToggleShotClock")]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private async Task ResetTimer()
    {
        IsTimerRunning = false;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("ResetShotClock")]
        };
        await SendAsync(req);
    }


    [RelayCommand]
    private async Task StartNewGame()
    {
        Player1Score = 0;
        Player2Score = 0;
        ActivePlayer = 1;
        IsTimerRunning = false;
        PendingDelta = 0;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("NewGame")]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private async Task EndGame()
    {
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("EndGame")]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private static async Task NavigateToAddPlayer() => await Shell.Current.GoToAsync("addscoreboardplayer");

    private Task SendAsync(ScoreBoardRequest request)
    {
        if (_connection.State != PiConnectionState.Connected) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        return _connection.SendMessageAsync(json);
    }
}
