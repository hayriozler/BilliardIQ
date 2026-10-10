using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public enum ManageKind
{
    Tables,
    Players,
    Teams,
    Clubs
}

public sealed record ManageRow(string Title, string Subtitle, string Badge, Color BadgeColor, ImageSource? Image = null, int Id = 0)
{
    public bool HasBadge => Badge.Length > 0;

    public bool HasImage => Image is not null;

    public bool HasNoImage => Image is null;

    public string Initial => Title.Length > 0 ? Title[..1].ToUpperInvariant() : "?";
}

public partial class ManageListPageModel(ApiClient api, AvatarImageService avatars, SessionStore session) : BasePageModel
{
    private static readonly Color _green = Color.FromArgb("#2E7D32");
    private static readonly Color _blue = Color.FromArgb("#1565C0");
    private static readonly Color _orange = Color.FromArgb("#EF6C00");
    private static readonly Color _grey = Color.FromArgb("#616161");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdd))]
    public partial ManageKind Kind { get; set; } = ManageKind.Tables;

    [ObservableProperty]
    public partial IReadOnlyList<ManageRow> Rows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool CanAdd => session.IsAdmin && Kind != ManageKind.Tables;

    [RelayCommand]
    private Task Appearing()
    {
        OnPropertyChanged(nameof(CanAdd));
        return LoadAsync();
    }

    [RelayCommand]
    private Task Add()
    {
        if (!session.IsAdmin)
        {
            return Task.CompletedTask;
        }

        return Kind switch
        {
            ManageKind.Players => Shell.Current.GoToAsync("playernew"),
            ManageKind.Teams => Shell.Current.GoToAsync("teamnew"),
            ManageKind.Clubs => Shell.Current.GoToAsync("clubnew"),
            _ => Task.CompletedTask
        };
    }

    [RelayCommand]
    private Task Open(ManageRow row) =>
        Kind == ManageKind.Players && row.Id > 0 ? Shell.Current.GoToAsync($"playeredit?playerId={row.Id}") : Task.CompletedTask;

    [RelayCommand]
    private Task Show(ManageKind kind)
    {
        Kind = kind;
        return LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            Rows = Kind switch
            {
                ManageKind.Tables => (await api.GetAsync<List<ManageTableDto>>("api/mobile/manage/tables")).Select(ToRow).ToList(),
                ManageKind.Players => await Task.WhenAll((await api.GetAsync<List<ManagePlayerDto>>("api/mobile/manage/players")).Where(p => !p.IsSystem).Select(ToRowAsync)),
                ManageKind.Teams => await Task.WhenAll((await api.GetAsync<List<ManageTeamDto>>("api/mobile/manage/teams")).Select(ToRowAsync)),
                _ => (await api.GetAsync<List<ManageClubDto>>("api/mobile/manage/clubs")).Select(ToRow).ToList()
            };
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            Rows = [];
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ManageRow ToRow(ManageTableDto t)
    {
        var title = string.IsNullOrWhiteSpace(t.Label) ? $"{L["Manage_Table"]} {t.Number}" : $"{t.Number} · {t.Label}";
        var subtitle = t.ScoreboardNo is { } no ? $"{L["Manage_ScoreboardNo"]} {no}" : L["Manage_NoScoreboard"];
        var (badge, color) = t.Status switch
        {
            0 => (L["Manage_Available"], _green),
            1 => (L["Manage_InUse"], _blue),
            2 => (L["Manage_Reserved"], _orange),
            3 => (L["Manage_Maintenance"], _grey),
            _ => (L["Manage_OutOfService"], _grey)
        };
        return new ManageRow(title, subtitle, badge, color);
    }

    private ManageRow ToRow(ManageClubDto c) =>
        new(c.Name, c.City ?? "", c.ShortName, _blue, Id: c.Id);

    private async Task<ManageRow> ToRowAsync(ManagePlayerDto p)
    {
        var subtitle = p.Name == p.Nickname ? "" : p.Name;
        var badge = p.ShortcutNumber is { } n ? $"#{n}" : "";
        return new ManageRow(p.Nickname, subtitle, badge, _blue, await avatars.GetAsync(p.AvatarId, p.PhotoPath), p.Id);
    }

    private async Task<ManageRow> ToRowAsync(ManageTeamDto t) =>
        new(t.Name, string.Join(", ", t.Players.Select(p => p.Nickname)), t.Players.Count.ToString(), _blue, await avatars.GetAsync(t.AvatarId, null));
}
