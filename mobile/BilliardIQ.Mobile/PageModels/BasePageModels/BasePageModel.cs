using BillardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BillardIQ.Mobile.PageModels.BasePageModels;

public abstract class BasePageModel : ObservableValidator
{
    public LocalizationManager L => LocalizationManager.Instance;
}
