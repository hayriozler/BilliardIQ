namespace BilliardIQ.Mobile.Services.Api;

public sealed class AvatarPickerSession
{
    private Action<int>? _onChosen;

    public void Begin(Action<int> onChosen) => _onChosen = onChosen;

    public void Complete(int avatarId)
    {
        _onChosen?.Invoke(avatarId);
        _onChosen = null;
    }
}
