namespace BilliardIQ.Mobile.Utilities;
public static class TaskUtilities
{
    public static async void FireAndForgetSafeAsync(this Task task) => await task;
}
