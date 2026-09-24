using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.PageModels.Admin;

public partial class PlayerListPageModel : BasePageModel
{
    private readonly ScoreboardPlayerSession _session;
    private readonly ScoreboardPlayerRepository _repository;
    private readonly IRaspberryPiConnectionService _connection;

    public PlayerListPageModel(ScoreboardPlayerSession session, ScoreboardPlayerRepository repository, IRaspberryPiConnectionService connection)
    {
        _session = session;
        _repository = repository;
        _connection = connection;
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
    private async Task DeleteAll()
    {
        if (Players.Count == 0) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            L["Admin_DeleteAllPlayers"], L["Admin_DeleteAllPlayersConfirm"], L["Action_Ok"], L["Action_Cancel"]);
        if (!confirmed) return;

        _session.Clear();
        await _repository.DeleteAllAsync();
    }
}
