using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;

namespace BilliardIQ.Mobile.Services.Api;

public sealed record ExternalMatchRequest(
    string PlayedOn, string OpponentName, string? Venue, int Score, int OpponentScore, int Innings, int HighRun, int? Outcome);

public sealed record ExternalMatchIdDto(int Id);

public sealed record PushResult(int? RemoteId, string? Rejection)
{
    public bool Succeeded => RemoteId is not null;

    public bool IsRejected => Rejection is not null;
}

public sealed class GameSyncService(ApiClient api, SessionStore session, GameRepository games)
{
    private const string _path = "api/mobile/player/external-matches";

    public bool IsEnabled => session.IsPlayer;

    public async Task<PushResult> PushAsync(Game game, int remoteId)
    {
        if (!IsEnabled)
        {
            return new PushResult(null, null);
        }

        var request = ToRequest(game);
        try
        {
            if (remoteId > 0)
            {
                try
                {
                    var updated = await api.PutAsync<ExternalMatchIdDto>($"{_path}/{remoteId}", request);
                    return new PushResult(updated.Id, null);
                }
                catch (ApiException ex) when (ex.StatusCode == 404)
                {
                }
            }

            var created = await api.PostAsync<ExternalMatchIdDto>(_path, request);
            return new PushResult(created.Id, null);
        }
        catch (ApiException ex) when (ex.StatusCode == 400)
        {
            return new PushResult(null, ex.Message);
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            return new PushResult(null, null);
        }
    }

    public async Task<bool> DeleteRemoteAsync(int remoteId)
    {
        if (!IsEnabled || remoteId <= 0)
        {
            return true;
        }

        try
        {
            await api.DeleteAsync($"{_path}/{remoteId}");
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return true;
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            return false;
        }
    }

    public async Task SyncPendingAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var game in await games.GetUnsyncedGamesAsync())
        {
            var result = await PushAsync(game, 0);
            if (result.RemoteId is { } id)
            {
                await games.SetRemoteIdAsync(game.Id, id);
            }
            else if (!result.IsRejected)
            {
                return;
            }
        }
    }

    private static ExternalMatchRequest ToRequest(Game game)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var played = DateOnly.FromDateTime(game.Date);
        return new ExternalMatchRequest(
            (played > today ? today : played).ToString("yyyy-MM-dd"),
            game.Opponent?.Trim() ?? "",
            string.IsNullOrWhiteSpace(game.Location) ? null : game.Location.Trim(),
            game.PlayerScore,
            game.OpponentScore,
            Math.Max(1, (int)Math.Ceiling(game.Innings)),
            game.HighestRun,
            null);
    }
}
