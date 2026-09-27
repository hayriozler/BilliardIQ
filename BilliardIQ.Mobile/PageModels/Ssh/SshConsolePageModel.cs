using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Ssh;

public partial class SshConsolePageModel : BasePageModel
{
    // Host/port/username live in SQLite; the password is kept out of the DB and stored in
    // platform SecureStorage (Keychain/KeyStore/DPAPI) instead.
    private const string PasswordStorageKey = "ssh_password";

    private readonly SshConnectionRepository _repository;
    private readonly ISshClientService _sshClient;
    private readonly IErrorHandler _errorHandler;

    public SshConsolePageModel(SshConnectionRepository repository, ISshClientService sshClient, IErrorHandler errorHandler)
    {
        _repository = repository;
        _sshClient = sshClient;
        _errorHandler = errorHandler;
    }

    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PortText { get; set; } = "22";

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Command { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Output { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task Appearing()
    {
        var settings = await _repository.GetAsync();
        if (settings is not null)
        {
            Host = settings.Host;
            PortText = settings.Port.ToString();
            Username = settings.Username;
        }

        try
        {
            Password = await SecureStorage.Default.GetAsync(PasswordStorageKey) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }

    [RelayCommand]
    private async Task SaveCredentials()
    {
        await _repository.SaveAsync(new SshConnectionSettings
        {
            Host = Host.Trim(),
            Port = ParsePort(),
            Username = Username.Trim(),
        });

        try
        {
            await SecureStorage.Default.SetAsync(PasswordStorageKey, Password);
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }

    [RelayCommand]
    private async Task RunCommand()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Command)) return;

        IsBusy = true;
        Output = string.Empty;
        try
        {
            Output = await _sshClient.RunCommandAsync(Host.Trim(), ParsePort(), Username.Trim(), Password, Command.Trim());
        }
        catch (Exception ex)
        {
            Output = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private int ParsePort() => int.TryParse(PortText, out var port) && port > 0 ? port : 22;
}
