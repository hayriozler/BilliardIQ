using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BilliardIQ.Mobile.PageModels.BasePageModels;

public abstract class BasePageModel : ObservableValidator
{
    public LocalizationManager L => LocalizationManager.Instance;
}
