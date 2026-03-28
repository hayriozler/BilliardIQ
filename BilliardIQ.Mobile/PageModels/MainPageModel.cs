using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels;

public partial class MainPageModel : BasePageModel
{
    [RelayCommand]
    private async Task TakePhoto() => await Shell.Current.GoToAsync("camera");
}

