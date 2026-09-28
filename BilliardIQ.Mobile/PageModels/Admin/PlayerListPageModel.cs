using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.PageModels.Admin;

public partial class PlayerListPageModel : BasePageModel
{
    private readonly ScoreboardPlayerSession _session;
    private readonly ScoreboardPlayerRepository _repository;
    private readonly IRaspberryPiConnectionService _connection;
    private readonly IErrorHandler _errorHandler;

    public PlayerListPageModel(ScoreboardPlayerSession session, ScoreboardPlayerRepository repository, IRaspberryPiConnectionService connection, IErrorHandler errorHandler)
    {
        _session = session;
        _repository = repository;
        _connection = connection;
        _errorHandler = errorHandler;
    }

    public ObservableCollection<ScoreboardPlayer> Players => _session.Players;

    [RelayCommand]
    private async Task Appearing()
    {
        if (_connection.State != PiConnectionState.Connected)
            await Shell.Current.GoToAsync("//connect");
    }

    [RelayCommand]
    private async Task AddPlayer() => await Shell.Current.GoToAsync("addscoreboardplayer");

    [RelayCommand]
    private async Task SelectPlayer(ScoreboardPlayer player)
    {
        await Shell.Current.GoToAsync("addscoreboardplayer", new Dictionary<string, object>
        {
            { "playerId", player.Id }
        });
    }

    [RelayCommand]
    private async Task DeletePlayer(ScoreboardPlayer? player)
    {
        if (player is null) return;

        if (player.IsDefaultPlayer)
        {
            await Shell.Current.DisplayAlertAsync(L["Admin_DeletePlayer"], L["Admin_DefaultPlayerDeleteBlocked"], L["Action_Ok"]);
            return;
        }

        var name = string.IsNullOrWhiteSpace(player.NickName) ? player.Name : player.NickName;
        var confirmed = await Shell.Current.DisplayAlertAsync(
            L["Admin_DeletePlayer"], string.Format(L["Admin_DeletePlayerConfirm"], name), L["Action_Ok"], L["Action_Cancel"]);
        if (!confirmed) return;

        if (!await SendDeletePlayerToRemoteAsync(player.Id)) return;

        // Post the removal to a fresh UI-dispatcher tick rather than mutating the CollectionView's
        // bound collection immediately after DisplayAlertAsync's ContentDialog closes — doing it
        // synchronously races the dialog's own teardown on Windows and can NullReferenceException.
        MainThread.BeginInvokeOnMainThread(() => _session.Remove(player));
        await _repository.DeleteAsync(player.Id);
    }

    private async Task<bool> SendDeletePlayerToRemoteAsync(int id)
    {
        if (_connection.State != PiConnectionState.Connected)
        {
            _errorHandler.HandleError(new InvalidOperationException("Not connected to the scoreboard."));
            return false;
        }

        try
        {
            var request = new ScoreBoardRequest
            {
                Type = "command",
                Commands = [new("DeletePlayer", id)]
            };
            await _connection.SendMessageAsync(JsonSerializer.Serialize(request, _jsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            return false;
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
