using CommunityToolkit.Mvvm.Input;
using GoatVaultInfrastructure.Services.Api.Models;
using Mopups.Pages;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace GoatVaultClient.Controls.Popups;

public partial class ErrorPopup : PopupPage
{
    public string Message { get; }
    public ObservableCollection<string> Errors { get; } = [];

    public IRelayCommand CloseCommand { get; }

    public ErrorPopup(string message, IEnumerable<string>? errors = null)
    {
        InitializeComponent();
        Message = message;
        if (errors != null)
        {
            foreach (var error in errors)
            {
                Errors.Add(error);
            }
        }

        CloseCommand = new RelayCommand(async () => await MopupService.Instance.PopAsync());
        BindingContext = this;
    }

    public ErrorPopup(ApiException ex) : this(GetFriendlyMessage(ex), ExtractErrors(ex))
    {
    }

    private static string GetFriendlyMessage(ApiException ex)
    {
        return ex.StatusCode switch
        {
            400 => "Invalid request. Please check your input.",
            401 => "Session expired or invalid credentials.",
            403 => "Access denied. You don't have permission.",
            404 => "The requested resource was not found.",
            409 => "A conflict occurred. The resource may already exist.",
            422 => "Please fix the validation errors below.",
            >= 500 => "A server error occurred. Please try again later.",
            _ => ex.Message
        };
    }

    private static IEnumerable<string> ExtractErrors(ApiException ex)
    {
        if (ex.Errors?.Detail != null)
        {
            return ex.Errors.Detail.Select(e =>
            {
                // Join location parts (e.g. "body", "email") with dots, skipping the first part if it's "body" or similar common root if desired.
                // For now, just join them all.
                var loc = string.Join(".", e.Loc);
                return string.IsNullOrEmpty(loc) ? e.Msg : $"{loc}: {e.Msg}";
            });
        }
        return [];
    }
}
