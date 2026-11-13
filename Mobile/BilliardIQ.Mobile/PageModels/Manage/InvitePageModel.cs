using BilliardIQ.Mobile.Services.Api;
using BilliardIQ.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;

namespace BilliardIQ.Mobile.PageModels.Manage;

public partial class InvitePageModel(ApiClient api) : BasePageModel
{
    private byte[]? _qrPng;

    [ObservableProperty]
    public partial IReadOnlyList<InvitablePlayerDto> Players { get; set; } = [];

    [ObservableProperty]
    public partial InvitablePlayerDto? SelectedPlayer { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExisting))]
    public partial bool IsNewPlayer { get; set; }

    [ObservableProperty]
    public partial string NewName { get; set; } = "";

    [ObservableProperty]
    public partial string NewNickname { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial string Code { get; set; } = "";

    [ObservableProperty]
    public partial string ResultTitle { get; set; } = "";

    [ObservableProperty]
    public partial string ExpiresText { get; set; } = "";

    [ObservableProperty]
    public partial ImageSource? QrImage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public bool IsExisting => !IsNewPlayer;

    public bool HasResult => Code.Length > 0;

    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private Task Appearing() => LoadAsync();

    [RelayCommand]
    private void ShowExisting() => Switch(false);

    [RelayCommand]
    private void ShowNew() => Switch(true);

    private void Switch(bool isNewPlayer)
    {
        if (IsNewPlayer == isNewPlayer)
        {
            return;
        }

        IsNewPlayer = isNewPlayer;
        ErrorMessage = "";
        Code = "";
        ResultTitle = "";
        ExpiresText = "";
        QrImage = null;
        _qrPng = null;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            Players = await api.GetAsync<List<InvitablePlayerDto>>("api/mobile/players/invitable");
            if (SelectedPlayer is { } current && Players.All(p => p.Id != current.Id))
            {
                SelectedPlayer = null;
            }
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            Players = [];
            ErrorMessage = ApiErrorText.For(ex, L);
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        ErrorMessage = "";
        if (IsNewPlayer ? NewName.Trim().Length == 0 && NewNickname.Trim().Length == 0 : SelectedPlayer is null)
        {
            ErrorMessage = L[IsNewPlayer ? "Invite_NameRequired" : "Invite_PickRequired"];
            return;
        }

        IsBusy = true;
        try
        {
            var invite = IsNewPlayer
                ? await api.PostAsync<InviteDto>("api/mobile/players/invite-new", new { Name = NewName.Trim(), Nickname = NewNickname.Trim() })
                : await api.PostAsync<InviteDto>($"api/mobile/players/{SelectedPlayer!.Id}/invite", null);
            Show(invite);
            NewName = "";
            NewNickname = "";
            await LoadAsync();
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Show(InviteDto invite)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(InviteLink.Build(invite.Code), QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        _qrPng = png;
        QrImage = ImageSource.FromStream(() => new MemoryStream(png));
        Code = invite.Code;
        ResultTitle = invite.PlayerName;
        ExpiresText = string.Format(L["Invite_Expires"], invite.ExpiresAt.ToLocalTime().ToString("g"));
    }

    [RelayCommand]
    private Task ShareCode() =>
        Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = L["Invite_Title"],
            Text = string.Format(L["Invite_ShareText"], ResultTitle, Code, InviteLink.Build(Code))
        });

    [RelayCommand]
    private async Task ShareQr()
    {
        if (_qrPng is null)
        {
            return;
        }

        var path = Path.Combine(FileSystem.CacheDirectory, $"invite-{Code}.png");
        await File.WriteAllBytesAsync(path, _qrPng);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = L["Invite_Title"],
            File = new ShareFile(path, "image/png")
        });
    }

    [RelayCommand]
    private async Task CopyCode()
    {
        await Clipboard.Default.SetTextAsync(Code);
        await AppShell.DisplayToastAsync(L["Invite_Copied"]);
    }
}
