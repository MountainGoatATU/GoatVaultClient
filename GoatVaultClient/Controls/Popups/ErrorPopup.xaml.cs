using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.API;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace GoatVaultClient.Controls.Popups;

public partial class ErrorPopup
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
        var errors = new List<string>();

        if (ex.Errors?.Detail != null)
        {
            errors.AddRange(ex.Errors.Detail.Select(e =>
            {
                var loc = string.Join(".", e.Loc);
                return string.IsNullOrEmpty(loc) ? e.Msg : $"{loc}: {e.Msg}";
            }));
        }

        // If no validation errors, include the raw message as detail if available.
        // This ensures the user sees the specific error (e.g. "Response status code...") 
        // underneath the friendly title.
        if (errors.Count == 0 && !string.IsNullOrWhiteSpace(ex.Message))
        {
             errors.Add(ex.Message);
        }

        return errors;
    }
}
