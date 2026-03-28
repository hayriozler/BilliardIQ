using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Utilities;
/// <summary>
/// Task Utilities.
/// </summary>
public static class TaskUtilities
{
    public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler? handler = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            handler?.HandleError(ex);
        }
    }   
}