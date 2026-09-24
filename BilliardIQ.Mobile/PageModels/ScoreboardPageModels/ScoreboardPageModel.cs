using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.PageModels.ScoreboardPageModels;

public partial class ScoreboardPageModel : BasePageModel
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRaspberryPiConnectionService _connection;
    private readonly ScoreboardPlayerSession _playerSession;
    private readonly ScoreboardPlayerRepository _playerRepository;
    private readonly TeamSession _teamSession;
    private readonly TeamRepository _teamRepository;
    private readonly MatchResultRepository _matchResultRepository;
    private readonly IErrorHandler _errorHandler;

    public ScoreboardPageModel(
        IRaspberryPiConnectionService connection,
        ScoreboardPlayerSession playerSession,
        ScoreboardPlayerRepository playerRepository,
        TeamSession teamSession,
        TeamRepository teamRepository,
        MatchResultRepository matchResultRepository,
        IErrorHandler errorHandler)
    {
        _connection = connection;
        _playerSession = playerSession;
        _playerRepository = playerRepository;
        _teamSession = teamSession;
        _teamRepository = teamRepository;
        _matchResultRepository = matchResultRepository;
        _errorHandler = errorHandler;
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
        if (_connection.State != PiConnectionState.Connected)
        {
            await Shell.Current.GoToAsync("//connect");
            return;
        }

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
        string? type;
        try
        {
            using var doc = JsonDocument.Parse(message);
            type = doc.RootElement.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        }
        catch (JsonException)
        {
            return;
        }

        switch (type)
        {
            case "state": HandleStateMessage(message); break;
            case "players": HandlePlayersMessage(message); break;
            case "teams": HandleTeamsMessage(message); break;
            case "matchResult": HandleMatchResultMessage(message); break;
        }
    }

    private void HandleStateMessage(string message)
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

        if (envelope?.State is not { } state) return;

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

    // Players/teams pushed from the Pi are matched to local records by RemoteId: update in place if
    // something changed, skip if not, insert a new local record if no match exists yet.
    private void HandlePlayersMessage(string message)
    {
        PlayersEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<PlayersEnvelope>(message, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope?.Players is not { } players) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var payload in players)
            {
                if (payload.RemoteId is { } remoteId) UpsertPlayer(remoteId, payload);
            }
        });
    }

    private void UpsertPlayer(int remoteId, PlayerSyncPayload payload)
    {
        var teamId = payload.TeamId is { } remoteTeamId ? _teamSession.FindByRemoteId(remoteTeamId)?.Id : null;
        var existing = _playerSession.FindByRemoteId(remoteId);

        if (existing is not null)
        {
            var nickName = payload.NickName ?? existing.NickName;
            var name = payload.Name ?? existing.Name;
            var avatar = payload.Avatar ?? existing.AvatarKey;
            var shortcut = payload.ShortcutNumber ?? existing.ShortcutNumber;

            var changed = existing.NickName != nickName
                || existing.Name != name
                || existing.AvatarKey != avatar
                || existing.TeamId != teamId
                || existing.ShortcutNumber != shortcut;
            if (!changed) return;

            existing.NickName = nickName;
            existing.Name = name;
            existing.AvatarKey = avatar;
            existing.TeamId = teamId;
            existing.ShortcutNumber = shortcut;
            _playerRepository.UpsertAsync(existing).FireAndForgetSafeAsync(_errorHandler);
            return;
        }

        var player = new ScoreboardPlayer
        {
            Id = _playerSession.NextId(),
            RemoteId = remoteId,
            NickName = payload.NickName ?? string.Empty,
            Name = payload.Name ?? string.Empty,
            AvatarKey = payload.Avatar,
            TeamId = teamId,
            ShortcutNumber = payload.ShortcutNumber,
        };
        _playerSession.Add(player);
        _playerRepository.UpsertAsync(player).FireAndForgetSafeAsync(_errorHandler);
    }

    private sealed class PlayersEnvelope
    {
        [JsonPropertyName("players")] public List<PlayerSyncPayload>? Players { get; set; }
    }
    private sealed class PlayerSyncPayload
    {
        [JsonPropertyName("remoteId")] public int? RemoteId { get; set; }
        [JsonPropertyName("nickName")] public string? NickName { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("avatar")] public string? Avatar { get; set; }
        [JsonPropertyName("teamId")] public int? TeamId { get; set; }
        [JsonPropertyName("shortcutNumber")] public int? ShortcutNumber { get; set; }
    }

    private void HandleTeamsMessage(string message)
    {
        TeamsEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<TeamsEnvelope>(message, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope?.Teams is not { } teams) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var payload in teams)
            {
                if (payload.RemoteId is { } remoteId && payload.Name is { } name) UpsertTeam(remoteId, name);
            }
        });
    }

    private void UpsertTeam(int remoteId, string name)
    {
        var existing = _teamSession.FindByRemoteId(remoteId);
        if (existing is not null)
        {
            if (existing.Name == name) return;

            existing.Name = name;
            _teamRepository.UpsertAsync(existing).FireAndForgetSafeAsync(_errorHandler);
            return;
        }

        var team = new ScoreboardTeam { Id = _teamSession.NextId(), RemoteId = remoteId, Name = name };
        _teamSession.Add(team);
        _teamRepository.UpsertAsync(team).FireAndForgetSafeAsync(_errorHandler);
    }

    private sealed class TeamsEnvelope
    {
        [JsonPropertyName("teams")] public List<TeamSyncPayload>? Teams { get; set; }
    }
    private sealed class TeamSyncPayload
    {
        [JsonPropertyName("remoteId")] public int? RemoteId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private void HandleMatchResultMessage(string message)
    {
        MatchResultEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MatchResultEnvelope>(message, _jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (envelope?.Result is not { } r) return;

        var result = new MatchResult
        {
            PlayedAt = r.PlayedAt,
            Player1Id = r.Player1Id,
            Player1Name = r.Player1Name,
            Player1Score = r.Player1Score,
            Player1Avg = r.Player1Avg,
            Player1HighRun = r.Player1HighRun,
            Player2Id = r.Player2Id,
            Player2Name = r.Player2Name,
            Player2Score = r.Player2Score,
            Player2Avg = r.Player2Avg,
            Player2HighRun = r.Player2HighRun,
            Inning = r.Inning,
            MatchTarget = r.MatchTarget,
            Winner = r.Winner,
        };
        _matchResultRepository.InsertAsync(result).FireAndForgetSafeAsync(_errorHandler);
    }

    private sealed class MatchResultEnvelope
    {
        [JsonPropertyName("result")] public MatchResultSyncPayload? Result { get; set; }
    }
    private sealed class MatchResultSyncPayload
    {
        [JsonPropertyName("playedAt")] public DateTime PlayedAt { get; set; }
        [JsonPropertyName("player1Id")] public int Player1Id { get; set; }
        [JsonPropertyName("player1Name")] public string Player1Name { get; set; } = "";
        [JsonPropertyName("player1Score")] public int Player1Score { get; set; }
        [JsonPropertyName("player1Avg")] public double Player1Avg { get; set; }
        [JsonPropertyName("player1HighRun")] public int Player1HighRun { get; set; }
        [JsonPropertyName("player2Id")] public int? Player2Id { get; set; }
        [JsonPropertyName("player2Name")] public string Player2Name { get; set; } = "";
        [JsonPropertyName("player2Score")] public int Player2Score { get; set; }
        [JsonPropertyName("player2Avg")] public double Player2Avg { get; set; }
        [JsonPropertyName("player2HighRun")] public int Player2HighRun { get; set; }
        [JsonPropertyName("inning")] public int Inning { get; set; }
        [JsonPropertyName("matchTarget")] public int MatchTarget { get; set; }
        [JsonPropertyName("winner")] public int Winner { get; set; }
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
    [NotifyPropertyChangedFor(nameof(IsPlayer1Active), nameof(IsPlayer2Active), nameof(Player1StrokeColor), nameof(Player2StrokeColor), nameof(Player1ArrowColor), nameof(Player2ArrowColor), nameof(DisplayedPlayer1Score), nameof(DisplayedPlayer2Score))]
    public partial int ActivePlayer { get; set; } = 1;

    public bool IsPlayer1Active => ActivePlayer == 1;
    public bool IsPlayer2Active => ActivePlayer == 2;
    public Color Player1StrokeColor => IsPlayer1Active ? Color.FromArgb("#2E7D32") : Colors.Transparent;
    public Color Player2StrokeColor => IsPlayer2Active ? Color.FromArgb("#F9A825") : Colors.Transparent;
    public Color Player1ArrowColor => IsPlayer1Active ? Color.FromArgb("#2E7D32") : Color.FromArgb("#9E9E9E");
    public Color Player2ArrowColor => IsPlayer2Active ? Color.FromArgb("#F9A825") : Color.FromArgb("#9E9E9E");

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
    private async Task IncrementActiveScore()
    {
        PendingDelta++;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("IncrementPoints")]
        };
        await SendAsync(req);
    }

    [RelayCommand]
    private async Task DecrementActiveScore()
    {
        var confirmed = IsPlayer1Active ? Player1Score : Player2Score;
        if (confirmed + PendingDelta <= 0) return;

        PendingDelta--;
        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("DecrementPoints")]
        };
        await SendAsync(req);
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
    private async Task PickPlayer1() => await PickPlayerAsync(1);

    [RelayCommand]
    private async Task PickPlayer2() => await PickPlayerAsync(2);

    [RelayCommand]
    private async Task ResolvePlayer1() => await ResolvePlayerAsync(1);

    [RelayCommand]
    private async Task ResolvePlayer2() => await ResolvePlayerAsync(2);

    private async Task PickPlayerAsync(int slot)
    {
        var players = _playerSession.Players;
        if (players.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync(L["Scoreboard_PickPlayer"], L["Scoreboard_NoPlayers"], L["Action_Ok"]);
            return;
        }

        var labels = players.Select(DisplayPlayerLabel).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(L["Scoreboard_PickPlayer"], L["Action_Cancel"], null, labels);
        var index = Array.IndexOf(labels, choice);
        if (index < 0) return;

        await ApplyPlayerAsync(slot, players[index]);
    }

    private async Task ResolvePlayerAsync(int slot)
    {
        var text = slot == 1 ? Player1Name : Player2Name;
        if (!int.TryParse(text?.Trim(), out var number)) return;

        var player = _playerSession.FindByShortcut(number) ?? _playerSession.FindById(number);
        if (player is not null) await ApplyPlayerAsync(slot, player);
    }

    private async Task ApplyPlayerAsync(int slot, ScoreboardPlayer player)
    {
        if (slot == 1) Player1Name = DisplayPlayerName(player);
        else Player2Name = DisplayPlayerName(player);

        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new($"SetPlayer{slot}", new
            {
                id = player.Id,
                remoteId = player.RemoteId,
                shortcutNumber = player.ShortcutNumber,
            })]
        };
        await SendAsync(req);
    }

    private static string DisplayPlayerName(ScoreboardPlayer player) =>
        string.IsNullOrWhiteSpace(player.NickName) ? player.Name : player.NickName;

    private static string DisplayPlayerLabel(ScoreboardPlayer player) =>
        player.ShortcutNumber is { } n ? $"#{n} {DisplayPlayerName(player)}" : DisplayPlayerName(player);

    private static readonly int[] _warmUpMinuteOptions = [5, 10, 15];

    [RelayCommand]
    private async Task StartWarmUp()
    {
        var labels = _warmUpMinuteOptions.Select(m => string.Format(L["Scoreboard_MinutesFormat"], m)).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(L["Scoreboard_WarmUpTitle"], L["Action_Cancel"], null, labels);
        var index = Array.IndexOf(labels, choice);
        if (index < 0) return;

        var req = new ScoreBoardRequest
        {
            Type = "command",
            Commands = [new("WarmUp", _warmUpMinuteOptions[index])]
        };
        await SendAsync(req);
    }

    private async Task SendAsync(ScoreBoardRequest request)
    {
        if (_connection.State != PiConnectionState.Connected)
        {
            _errorHandler.HandleError(new InvalidOperationException("Not connected to the scoreboard."));
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            await _connection.SendMessageAsync(json);
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }
}
