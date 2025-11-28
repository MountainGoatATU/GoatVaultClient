using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoatVaultClient_v3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class EducationPageViewModel : BaseViewModel
    {
        private readonly MarkdownHelperService _markdownHelperService;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private HtmlWebViewSource _htmlSource; 

        public EducationPageViewModel(MarkdownHelperService markdownHelperService)
        {
            _markdownHelperService = markdownHelperService;
        }

        public async Task LoadDocumentAsync()
        {
            if (IsLoading) return;

            try
            {
                // Load and convert the Markdown file to HTML
                string htmlContent = await _markdownHelperService.GetHtmlFromAssetAsync("intro.md");

                // Create a source for the webview
                HtmlSource = new HtmlWebViewSource
                {
                    Html = htmlContent
                };
            } catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, conversion errors)
                Console.WriteLine($"Error loading document: {ex.Message}");
            }
            finally
            {
                IsLoading = false;


            }
        }
    }
}
