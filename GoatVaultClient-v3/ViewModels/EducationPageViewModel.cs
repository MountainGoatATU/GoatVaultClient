using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoatVaultClient_v3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class EducationPageViewModel : BaseViewModel
    {
        private readonly MarkdownHelperService _markdownHelperService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private HtmlWebViewSource _htmlSource;

        [ObservableProperty]
        private EducationTopic _currentTopic;

        public ObservableCollection<EducationTopic> Topics { get; }

        public EducationPageViewModel(MarkdownHelperService markdownHelperService)
        {
            _markdownHelperService = markdownHelperService;

            Topics = new ObservableCollection<EducationTopic>
            {
                new EducationTopic
                {
                    Title = "Introduction",
                    FileName = "Structure.md",
                    Quiz = new QuizData
                    {
                        Title = "What is the purpose of the education structure?",
                        Questions = new List<QuizOption>
                        {
                            new QuizOption { Text = "To educate the user", IsCorrect = true },
                            new QuizOption { Text = "To waste users's time", IsCorrect = false },
                            new QuizOption { Text = "To provide misleading topics and incorrect information", IsCorrect = false }
                        }
                    }
                },
                new EducationTopic
                {
                    Title = "Shamir",
                    FileName = "Shamir.md",
                    Quiz = new QuizData
                    {
                        Title = "What is Shamir Secret Sharing?",
                        Questions = new List<QuizOption>
                        {
                            new QuizOption { Text = "Just some guy telling a secret to his frined", IsCorrect = true },
                            new QuizOption { Text = "Shamir does have a secret he wants to share", IsCorrect = false },
                            new QuizOption { Text = "A way on how to retrieve passwords from multiple shares by using interpolation", IsCorrect = false }
                        }
                    }
                }
            };

            _currentTopic = Topics.Last();
        }

        async partial void OnCurrentTopicChanged(EducationTopic value)
        {
            if (value != null)
            {
                await LoadTopicAsync(value);
            }
        }

        public async Task InitializeAsync()
        {
            if (HtmlSource == null)
            {
                await LoadTopicAsync(CurrentTopic);
            }
        }

        [RelayCommand]
        public async Task LoadTopicAsync(EducationTopic topic)
        {
            if (topic == null || IsLoading) return;

            try
            {
                IsLoading = true;

                // Load and convert the Markdown file to HTML
                string htmlContent = await _markdownHelperService.GetHtmlFromAssetAsync(topic.FileName, topic.Quiz);

                var source = new HtmlWebViewSource { Html = htmlContent };

                // Set the base URL for Android to access local assets
                source.BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "file:///android_asset/"
                    : null;

               HtmlSource = source;

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
