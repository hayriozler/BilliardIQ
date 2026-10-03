using System.Collections.ObjectModel;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class AvatarCell(int id) : ObservableObject
{
    public int Id { get; } = id;

    [ObservableProperty]
    public partial ImageSource? Image { get; set; }
}

public partial class AvatarPickerPageModel(ApiClient api, AvatarImageService avatars, AvatarPickerSession picker) : BasePageModel
{
    private bool _loaded;

    public ObservableCollection<AvatarCell> Cells { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private async Task Appearing()
    {
        if (_loaded)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var count = (await api.GetAsync<AvatarCountDto>("api/mobile/avatars/count")).Count;
            Cells.Clear();
            for (var i = 0; i < count; i++)
            {
                Cells.Add(new AvatarCell(i));
            }

            _loaded = true;
            foreach (var batch in Cells.Chunk(10))
            {
                await Task.WhenAll(batch.Select(async c => c.Image = await avatars.GetAsync(c.Id, null)));
            }
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Select(AvatarCell cell)
    {
        picker.Complete(cell.Id);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task Cancel() => Shell.Current.GoToAsync("..");
}
