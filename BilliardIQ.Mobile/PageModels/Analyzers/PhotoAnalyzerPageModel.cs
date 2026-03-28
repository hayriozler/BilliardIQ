
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BilliardIQ.Mobile.PageModels.Analyzers;

public partial class PhotoAnalyzerPageModel : BasePageModel
{
    private readonly ICameraProvider _cameraProvider;
    public PhotoAnalyzerPageModel(ICameraProvider CameraProvider)
    {
        _cameraProvider = CameraProvider;
        _cameraProvider.AvailableCamerasChanged += HandleAvailableCamerasChanged;
    }    
    private void HandleAvailableCamerasChanged(object? sender, IReadOnlyList<CameraInfo>? e) => OnPropertyChanged(nameof(Cameras));
    public IReadOnlyList<CameraInfo> Cameras => _cameraProvider.AvailableCameras ?? [];

    [ObservableProperty]
    public partial CameraInfo? SelectedCamera { get; set; }

    [ObservableProperty]
    public partial Size SelectedResolution { get; set; }

    [ObservableProperty]
    public partial CameraFlashMode FlashMode { get; set; }

    public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();
}
