namespace BilliardIQ.Mobile.Pages.BasePages;

public abstract class BasePage : ContentPage
{
    public BasePage(object? viewModel = null)
    {
        Padding = 12;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"Page {GetType().Name} is appearing.");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Console.WriteLine($"Page {GetType().Name} is disappearing.");
    }
}