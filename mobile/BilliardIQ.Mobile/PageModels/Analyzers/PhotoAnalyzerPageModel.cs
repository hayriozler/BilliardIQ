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
        var cams = Cameras;
        CameraInfo? found = null;
        for (int i = 0; i < cams.Count; i++)
        {
            if (cams[i].Position == CameraPosition.Rear)
            {
                found = cams[i];
                break;
            }
        }

        if (found is null && cams.Count > 0)
            found = cams[0];

        SelectedCamera = found;
        IsBackCameraSelected = true;
    }

    [RelayCommand]
    void SelectFrontCamera()
    {
        var cams = Cameras;
        CameraInfo? found = null;
        for (int i = 0; i < cams.Count; i++)
        {
            if (cams[i].Position == CameraPosition.Front)
            {
                found = cams[i];
                break;
            }
        }

        if (found is null && cams.Count > 0)
            found = cams[0];

        SelectedCamera = found;
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
