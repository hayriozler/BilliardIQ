using BilliardIQ.Mobile.ViewModels;

namespace BilliardIQ.Mobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
