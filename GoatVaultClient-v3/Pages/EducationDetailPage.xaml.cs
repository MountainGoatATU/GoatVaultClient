using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3;

public partial class EducationDetailPage : ContentPage
{
	public EducationDetailPage(EducationDetailViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        // Check if the navigation is our custom "signal" from the Javascript
        if (e.Url != null && e.Url.StartsWith("goatvault://"))
        {
            // 1. Cancel the actual navigation so the WebView doesn't try to load the fake URL
            e.Cancel = true;

            // 2. Parse the result (e.g., goatvault://quiz_complete?success=true)
            if (e.Url.Contains("quiz_complete"))
            {
                // Simple parsing logic
                bool isSuccess = e.Url.Contains("success=true");

                // 3. Pass to ViewModel (you can implement this method in your VM later)
                // _viewModel.OnQuizCompleted(isSuccess);

                // For now, just a debug log to prove it works
                Console.WriteLine($"Quiz Completed. Success: {isSuccess}");
            }
        }
    }
}