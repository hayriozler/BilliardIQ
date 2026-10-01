using BilliardIQ.Mobile.Utilities;

namespace BilliardIQ.Mobile.Services;

public class ModalErrorHandler : IErrorHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public void HandleError(Exception ex) => DisplayAlertAsync(ex).FireAndForgetSafeAsync();

    async Task DisplayAlertAsync(Exception ex)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (Shell.Current is Shell shell)
                await shell.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}