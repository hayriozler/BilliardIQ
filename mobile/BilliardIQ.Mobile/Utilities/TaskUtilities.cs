using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Utilities;

public static class TaskUtilities
{
    public static async void FireAndForgetSafeAsync(this Task task)
    {
        try { await task; }
        catch { }
    }

    public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler errorHandler)
    {
        try { await task; }
        catch (Exception ex) { errorHandler.HandleError(ex); }
    }
}
