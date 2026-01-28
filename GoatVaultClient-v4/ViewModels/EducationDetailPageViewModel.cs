using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultClient_v4.Models;
using GoatVaultClient_v4.Services;

namespace GoatVaultClient_v4.ViewModels;

// This attribute maps the navigation parameter "Topic" to the Property "SelectedTopic"
[QueryProperty(nameof(SelectedTopic), "Topic")]
public partial class EducationDetailViewModel : BaseViewModel
{
    private readonly MarkdownHelperService _markdownHelperService;

    [ObservableProperty] private EducationTopic _selectedTopic;
    [ObservableProperty] private HtmlWebViewSource _htmlSource;
    [ObservableProperty] private bool _isLoading;

    public EducationDetailViewModel(MarkdownHelperService markdownHelperService)
    {
        _markdownHelperService = markdownHelperService;
    }

    // Automatically called when SelectedTopic is set by the navigation
    async partial void OnSelectedTopicChanged(EducationTopic value)
    {
        if (value != null)
            await LoadTopicAsync(value);
    }

    private async Task LoadTopicAsync(EducationTopic topic)
    {
        try
        {
            IsLoading = true;
            var htmlContent = await _markdownHelperService.GetHtmlFromAssetAsync(topic.FileName, topic.Quiz);

            var source = new HtmlWebViewSource
            {
                Html = htmlContent,
                BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "file:///android_asset/"
                    : null
            };

            HtmlSource = source;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}