
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Analyzers;

public partial class PhotoAnalyzerPageModel : BasePageModel
{
    private readonly ICameraProvider _cameraProvider;
    public PhotoAnalyzerPageModel(ICameraProvider CameraProvider)
    {
        _cameraProvider = CameraProvider;
        _cameraProvider.AvailableCamerasChanged += HandleAvailableCamerasChanged;
    }

    private void HandleAvailableCamerasChanged(object? sender, IReadOnlyList<CameraInfo>? e)
    {
        OnPropertyChanged(nameof(Cameras));
        if (SelectedCamera is null)
            SelectBackCamera();
    }

    public IReadOnlyList<CameraInfo> Cameras => _cameraProvider.AvailableCameras ?? [];

    [ObservableProperty]
    public partial CameraInfo? SelectedCamera { get; set; }

    [ObservableProperty]
    public partial Size SelectedResolution { get; set; }

    [ObservableProperty]
    public partial bool IsBackCameraSelected { get; set; } = true;

    [RelayCommand]
    void SelectBackCamera()
    {
        SelectedCamera = Cameras.FirstOrDefault(c => c.Position == CameraPosition.Rear) ?? Cameras.FirstOrDefault();
        IsBackCameraSelected = true;
    }

    [RelayCommand]
    void SelectFrontCamera()
    {
        SelectedCamera = Cameras.FirstOrDefault(c => c.Position == CameraPosition.Front) ?? Cameras.FirstOrDefault();
        IsBackCameraSelected = false;
    }

    [RelayCommand]
    void ToggleCamera()
    {
        if (IsBackCameraSelected)
            SelectFrontCamera();
        else
            SelectBackCamera();
    }

    public event EventHandler? CaptureRequested;

    [RelayCommand]
    void StartCapture() => CaptureRequested?.Invoke(this, EventArgs.Empty);
}
