
using BillardIQ.Mobile.Utilities;

namespace BillardIQ.Mobile.Services;

public sealed class ShowAlertHandler : IAlertHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public async Task ShowAlertAsync(string titleKey, string messageKey, string cancelKey)
    {
        try
        {
            await _semaphore.WaitAsync();
            var l = LocalizationManager.Instance;
            if (Shell.Current is Shell shell)
                shell.DisplayAlertAsync(l[titleKey], l[messageKey], l[cancelKey]).FireAndForgetSafeAsync();
        }
        finally
        {
            _semaphore.Release();
        }
        
    }
}