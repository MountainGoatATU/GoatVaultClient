using GoatVaultClient.ViewModels;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.Pages;

public partial class EducationDetailPage : ContentPage
{
    private readonly ILogger<EducationDetailPage>? _logger;

    public EducationDetailPage(EducationDetailViewModel vm, ILogger<EducationDetailPage>? logger = null)
    {
        InitializeComponent();
        BindingContext = vm;
        _logger = logger;
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        // Check if the navigation is our custom "signal" from the Javascript
        if (!e.Url.StartsWith("goatvault://"))
            return;

        // 1. Cancel the actual navigation so the WebView doesn't try to load the fake URL
        e.Cancel = true;

        // 2. Parse the result (e.g., goatvault://quiz_complete?success=true)
        if (!e.Url.Contains("quiz_complete"))
            return;

        // Simple parsing logic
        var isSuccess = e.Url.Contains("success=true");

        // 3. Pass to ViewModel (you can implement this method in your VM later)
        // _viewModel.OnQuizCompleted(isSuccess);

        _logger?.LogDebug("Quiz completed (Success: {IsSuccess})", isSuccess);
    }
}
