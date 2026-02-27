using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultClient.Controls.Popups;
using GoatVaultInfrastructure.Services.Api.Models;
using Mopups.Services;

namespace GoatVaultClient.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))] 
    private bool _isBusy;
    [ObservableProperty] private string _title = string.Empty;

    // Helper property for XAML (e.g., IsEnabled="{Binding IsNotBusy}")
    public bool IsNotBusy => !IsBusy;

    protected async Task SafeExecuteAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (ApiException ex)
        {
            await MopupService.Instance.PushAsync(new ErrorPopup(ex));
        }
#if ANDROID
        catch (Java.Lang.Exception javaEx)
        {
            if (javaEx.Message != null)
                await MopupService.Instance.PushAsync(new ErrorPopup("A platform error occurred", [javaEx.Message]));
        }
#endif
        catch (Exception ex)
        {
            await MopupService.Instance.PushAsync(new ErrorPopup("An unexpected error occurred", [ex.Message]));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
