using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public enum ManageKind
{
    Tables,
    Players,
    Teams
}

public sealed record ManageRow(string Title, string Subtitle, string Badge, Color BadgeColor, ImageSource? Image = null)
{
    public bool HasBadge => Badge.Length > 0;

    public bool HasImage => Image is not null;

    public bool HasNoImage => Image is null;

    public string Initial => Title.Length > 0 ? Title[..1].ToUpperInvariant() : "?";
}

public partial class ManageListPageModel(ApiClient api, AvatarImageService avatars) : BasePageModel
{
    private static readonly Color _green = Color.FromArgb("#2E7D32");
    private static readonly Color _blue = Color.FromArgb("#1565C0");
    private static readonly Color _orange = Color.FromArgb("#EF6C00");
    private static readonly Color _grey = Color.FromArgb("#616161");

    [ObservableProperty]
    public partial ManageKind Kind { get; set; } = ManageKind.Tables;

    [ObservableProperty]
    public partial IReadOnlyList<ManageRow> Rows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private Task Appearing() => LoadAsync();

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
                _ => await Task.WhenAll((await api.GetAsync<List<ManageTeamDto>>("api/mobile/manage/teams")).Select(ToRowAsync))
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

    private async Task<ManageRow> ToRowAsync(ManagePlayerDto p)
    {
        var subtitle = p.Name == p.Nickname ? "" : p.Name;
        var badge = p.ShortcutNumber is { } n ? $"#{n}" : "";
        return new ManageRow(p.Nickname, subtitle, badge, _blue, await avatars.GetAsync(p.AvatarId, p.PhotoPath));
    }

    private async Task<ManageRow> ToRowAsync(ManageTeamDto t) =>
        new(t.Name, string.Join(", ", t.Players.Select(p => p.Nickname)), t.Players.Count.ToString(), _blue, await avatars.GetAsync(t.AvatarId, null));
}
