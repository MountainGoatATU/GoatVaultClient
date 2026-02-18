using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GoatVaultClient.Pages;
using GoatVaultCore.Models;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class EducationPageViewModel(ILogger<EducationPageViewModel>? logger = null) : BaseViewModel
{
    public ObservableCollection<EducationTopic> Topics { get; } =
    [
        new()
        {
            Title = "Introduction", FileName = "Structure.md",
            Quiz = new QuizData
            {
                Title = "What is the purpose of the education structure?",
                Questions =
                [
                    new QuizOption { Text = "To educate the user", IsCorrect = true },
                    new QuizOption { Text = "To waste users's time", IsCorrect = false },
                    new QuizOption
                        { Text = "To provide misleading topics and incorrect information", IsCorrect = false }
                ]
            }
        },

        new()
        {
            Title = "Shamir", FileName = "Shamir.md",
            Quiz = new QuizData
            {
                Title = "What is Shamir Secret Sharing?",
                Questions =
                [
                    new QuizOption { Text = "Just some guy telling a secret to his friend", IsCorrect = true },
                    new QuizOption { Text = "Shamir does have a secret he wants to share", IsCorrect = false },
                    new QuizOption
                    {
                        Text = "A way on how to retrieve passwords from multiple shares by using interpolation",
                        IsCorrect = false
                    }
                ]
            }
        }
    ];

    [ObservableProperty] private EducationTopic? _currentTopic;

    // Define your topics here as you did before

    [RelayCommand]
    public async Task OpenTopicAsync(EducationTopic? topic)
    {
        if (topic == null)
            return;

        try
        {
            // Navigate to the detail page and pass the 'topic' object
            var navigationParameter = new Dictionary<string, object>
            {
                { "Topic", topic }
            };

            // Resetting the topic in the menu
            CurrentTopic = null;

            await Shell.Current.GoToAsync(nameof(EducationDetailPage), navigationParameter);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error navigating to EducationDetailPage for topic {TopicTitle}", topic.Title);
        }
    }
}