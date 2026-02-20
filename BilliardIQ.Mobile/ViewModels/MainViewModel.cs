using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ImageSource? _selectedImage;

    [RelayCommand]
    async Task PickPhoto()
    {
        var result = await MediaPicker.Default.CapturePhotoAsync();

        if (result != null)
        {
            var stream = await result.OpenReadAsync();
            SelectedImage = ImageSource.FromStream(() => stream);
        }
    }
}
