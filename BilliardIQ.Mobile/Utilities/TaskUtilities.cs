using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Utilities;

public static class TaskUtilities
{
    // No error handler: exceptions are still observed (avoids an unobserved-task-exception
    // crash) but otherwise discarded. Prefer the IErrorHandler overload wherever a failure
    // should be visible to the user.
    public static async void FireAndForgetSafeAsync(this Task task)
    {
        try { await task; }
        catch { /* intentionally discarded */ }
    }

    public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler errorHandler)
    {
        try { await task; }
        catch (Exception ex) { errorHandler.HandleError(ex); }
    }
}
