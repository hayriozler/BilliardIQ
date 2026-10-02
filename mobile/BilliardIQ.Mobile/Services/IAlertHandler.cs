namespace BilliardIQ.Mobile.Services;

public interface IAlertHandler 
{
    Task ShowAlertAsync(string title, string message, string cancel);
}
