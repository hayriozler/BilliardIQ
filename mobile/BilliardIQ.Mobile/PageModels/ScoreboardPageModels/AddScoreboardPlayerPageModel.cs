using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilliardIQ.Mobile.PageModels.ScoreboardPageModels;

public partial class AddScoreboardPlayerPageModel : BasePageModel, IQueryAttributable
{
    private readonly ScoreboardPlayerSession _session;
    private readonly ScoreboardPlayerRepository _repository;
    private readonly IRaspberryPiConnectionService _connection;
    private readonly IErrorHandler _errorHandler;

    public AddScoreboardPlayerPageModel(ScoreboardPlayerSession session, ScoreboardPlayerRepository repository, TeamSession teamSession, IRaspberryPiConnectionService connection, IErrorHandler errorHandler)
    {
        _session = session;
        _repository = repository;
        _connection = connection;
        _errorHandler = errorHandler;
        Teams = teamSession.Teams;

        ErrorsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NickNameError));
            OnPropertyChanged(nameof(HasNickNameError));
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(HasNameError));
        };
    }

    public ObservableCollection<ScoreboardTeam> Teams { get; }

    [ObservableProperty]
    public partial ScoreboardTeam? SelectedTeam { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(PageTitle), nameof(SaveButtonText))]
    public partial int? EditingPlayerId { get; set; }

    public bool IsEditing => EditingPlayerId is not null;
    public string PageTitle => IsEditing ? L["AddPlayer_UpdateTitle"] : L["AddPlayer_Title"];
    public string SaveButtonText => IsEditing ? L["Action_Update"] : L["Action_Save"];

    [RelayCommand]
    private async Task Appearing()
    {
        if (_connection.State != PiConnectionState.Connected)
            await Shell.Current.GoToAsync("//connect");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("playerId", out var value) && value is int playerId)
        {
            var player = _session.FindById(playerId);
            if (player is not null) LoadPlayer(player);
        }
    }

    [RelayCommand]
    private async Task SelectExistingPlayer()
    {
        var players = _session.Players;
        if (players.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync(L["Scoreboard_PickPlayer"], L["Scoreboard_NoPlayers"], L["Action_Ok"]);
            return;
        }

        var labels = players.Select(DisplayPlayerLabel).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(L["Scoreboard_PickPlayer"], L["Action_Cancel"], null, labels);
        var index = Array.IndexOf(labels, choice);
        if (index < 0) return;

        LoadPlayer(players[index]);
    }

    private static string DisplayPlayerName(ScoreboardPlayer player) =>
        string.IsNullOrWhiteSpace(player.NickName) ? player.Name : player.NickName;

    private static string DisplayPlayerLabel(ScoreboardPlayer player) =>
        player.ShortcutNumber is { } n ? $"#{n} {DisplayPlayerName(player)}" : DisplayPlayerName(player);

    private void LoadPlayer(ScoreboardPlayer player)
    {
        EditingPlayerId = player.Id;
        SelectedAvatarOption = AvatarCatalog.Find(player.AvatarKey);
        Photo = player.Photo;
        PhotoSource = player.PhotoSource;
        NickName = player.NickName;
        Name = player.Name;
        ShortcutNumberText = player.ShortcutNumber?.ToString();
        SelectedTeam = Teams.FirstOrDefault(t => t.Id == player.TeamId);
        ClearErrors();
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Nickname is required")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? NickName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name is required")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? ShortcutNumberText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial byte[]? Photo { get; set; }

    [ObservableProperty]
    public partial ImageSource? PhotoSource { get; set; }

    [ObservableProperty]
    public partial AvatarOption? SelectedAvatarOption { get; set; }

    public bool HasPhoto => Photo is { Length: > 0 };

    public IReadOnlyList<AvatarOption> AvatarOptions => AvatarCatalog.Options;

    [RelayCommand]
    private async Task TakePhoto()
    {
        try
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return;
#endif
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return;

            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var originalBytes = ms.ToArray();

            var thumb = await Task.Run(() => ImagePreprocessor.CreateThumbnail(originalBytes, maxWidth: 300, maxHeight: 300));
            SetPhoto(thumb.Length > 0 ? thumb : originalBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Player photo camera error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        Photo = null;
        PhotoSource = null;
    }

    private void SetPhoto(byte[] bytes)
    {
        Photo = bytes;
        PhotoSource = ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        int? shortcutNumber = null;
        if (!string.IsNullOrWhiteSpace(ShortcutNumberText))
        {
            if (!int.TryParse(ShortcutNumberText.Trim(), out var parsed) || parsed <= 0)
            {
                await Shell.Current.DisplayAlertAsync(L["AddPlayer_ShortcutNumber"], L["AddPlayer_ShortcutNumberInvalid"], L["Action_Ok"]);
                return;
            }

            var conflict = _session.FindByShortcut(parsed);
            if (conflict is not null && conflict.Id != EditingPlayerId)
            {
                await Shell.Current.DisplayAlertAsync(L["AddPlayer_ShortcutNumber"], string.Format(L["AddPlayer_ShortcutNumberTaken"], DisplayPlayerName(conflict)), L["Action_Ok"]);
                return;
            }

            shortcutNumber = parsed;
        }

        // RemoteId isn't user-editable — it's populated once the Pi acknowledges the player,
        // so preserve whatever the existing record already has when editing.
        var remoteId = EditingPlayerId is { } editingId ? _session.FindById(editingId)?.RemoteId : null;

        var player = new ScoreboardPlayer
        {
            Id = EditingPlayerId ?? _session.NextId(),
            RemoteId = remoteId,
            NickName = NickName!.Trim(),
            Name = Name!.Trim(),
            Photo = Photo,
            AvatarKey = SelectedAvatarOption?.Key,
            TeamId = SelectedTeam?.Id,
            ShortcutNumber = shortcutNumber,
        };

        if (await SendPlayerToRemoteAsync(player))
        {
            var existing = EditingPlayerId is not null ? _session.FindById(player.Id) : null;
            if (existing is not null)
            {
                existing.RemoteId = player.RemoteId;
                existing.NickName = player.NickName;
                existing.Name = player.Name;
                existing.Photo = player.Photo;
                existing.AvatarKey = player.AvatarKey;
                existing.TeamId = player.TeamId;
                existing.ShortcutNumber = player.ShortcutNumber;
            }
            else
            {
                _session.Add(player);
            }

            await _repository.UpsertAsync(player);

            ResetForm();
            await Shell.Current.GoToAsync("..");
        }
    }

    private void ResetForm()
    {
        EditingPlayerId = null;
        NickName = null;
        Name = null;
        ShortcutNumberText = null;
        Photo = null;
        PhotoSource = null;
        SelectedAvatarOption = null;
        SelectedTeam = null;
        ClearErrors();
    }

    private bool CanSave() => !HasErrors;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private async Task<bool> SendPlayerToRemoteAsync(ScoreboardPlayer player)
    {
        if (_connection.State != PiConnectionState.Connected)
        {
            _errorHandler.HandleError(new InvalidOperationException("Not connected to the scoreboard."));
            return false;
        }

        try
        {
            var (imageBytes, imageExtension) = await ResolveImageAsync(player);
            var command = new ScoreBoardCommand("AddPlayer", new
            {
                id = player.Id,
                remoteId = player.RemoteId,
                nickName = player.NickName,
                name = player.Name,
                avatar = player.AvatarKey,
                teamId = player.TeamId,
                shortcutNumber = player.ShortcutNumber,
                photoBase64 = imageBytes is { Length: > 0 } ? Convert.ToBase64String(imageBytes) : null,
                photoExtension = imageBytes is { Length: > 0 } ? imageExtension : null,
            });
            await _connection.SendMessageAsync(JsonSerializer.Serialize(command, _jsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            return false;
        }
    }

    private async Task<(byte[]? Bytes, string? Extension)> ResolveImageAsync(ScoreboardPlayer player)
    {
        if (player.Photo is { Length: > 0 } photo)
            return (photo, "jpg");

        var avatarOption = AvatarCatalog.Find(player.AvatarKey);
        if (avatarOption is null) return (null, null);

        var rendered = await AvatarRenderer.RenderPngAsync(avatarOption.FileName);
        return rendered.Length > 0 ? (rendered, "png") : (null, null);
    }

    public string? NickNameError => GetErrors(nameof(NickName)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasNickNameError => GetErrors(nameof(NickName)).Cast<object>().Any();
    public string? NameError => GetErrors(nameof(Name)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasNameError => GetErrors(nameof(Name)).Cast<object>().Any();
}
