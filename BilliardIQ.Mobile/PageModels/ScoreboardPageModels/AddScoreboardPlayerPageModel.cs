using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.ScoreboardPageModels;

public partial class AddScoreboardPlayerPageModel : BasePageModel
{
    private readonly ScoreboardPlayerSession _session;
    private readonly IRaspberryPiConnectionService _connection;
    private readonly IErrorHandler _errorHandler;

    public AddScoreboardPlayerPageModel(ScoreboardPlayerSession session, IRaspberryPiConnectionService connection, IErrorHandler errorHandler)
    {
        _session = session;
        _connection = connection;
        _errorHandler = errorHandler;

        ErrorsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NickNameError));
            OnPropertyChanged(nameof(HasNickNameError));
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(HasNameError));
        };
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
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial byte[]? Photo { get; set; }

    [ObservableProperty]
    public partial ImageSource? PhotoSource { get; set; }

    public bool HasPhoto => Photo is { Length: > 0 };

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

        var player = new ScoreboardPlayer
        {
            Id = _session.NextId(),
            NickName = NickName!.Trim(),
            Name = Name!.Trim(),
            Photo = Photo,
        };

        if (await SendPlayerToRemoteAsync(player))
            await Shell.Current.GoToAsync("..");
    }

    private bool CanSave() => !HasErrors;

    private async Task<bool> SendPlayerToRemoteAsync(ScoreboardPlayer player)
    {
        if (_connection.State != PiConnectionState.Connected)
        {
            _errorHandler.HandleError(new InvalidOperationException("Not connected to the scoreboard."));
            return false;
        }

        try
        {
            var payload = $$"""{"type":"player_added","id":{{player.Id}},"nickName":"{{Escape(player.NickName)}}","name":"{{Escape(player.Name)}}"}""";
            await _connection.SendMessageAsync(payload);
            return true;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            return false;
        }
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public string? NickNameError => GetErrors(nameof(NickName)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasNickNameError => GetErrors(nameof(NickName)).Cast<object>().Any();
    public string? NameError => GetErrors(nameof(Name)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasNameError => GetErrors(nameof(Name)).Cast<object>().Any();
}
