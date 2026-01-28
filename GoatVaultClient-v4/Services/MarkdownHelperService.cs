using GoatVaultClient_v4.Models;
using Markdig;
using System.Text.Json;

namespace GoatVaultClient_v4.Services;

public interface IMarkdownHelperService
{
    Task<string> GetHtmlFromAssetAsync(string filename);
}
public class MarkdownHelperService
{
    public async Task<string> GetHtmlFromAssetAsync(string filename, QuizData? quizData = null)
    {
        // 1. Open the file from the Resources/Raw folder
        await using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
        using var reader = new StreamReader(stream);
        var markdownContent = await reader.ReadToEndAsync();

        var cssContent = await LoadAssetAsString("quiz.css");
        var jsContent = await LoadAssetAsString("quiz.js");

        // 2. Configure the pipeline (enable advanced features like tables)
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // 3. Convert Markdown to HTML
        var htmlBody = Markdown.ToHtml(markdownContent, pipeline);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var quizJson = "null";
        if (quizData != null)
        {
            quizJson = JsonSerializer.Serialize(quizData, jsonOptions);
        }

        // 4. Wrap it in a full HTML document with CSS styling
        return $"""

                                <!DOCTYPE html>
                                <html>
                                <head>
                                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                    <style>{cssContent}</style>
                                </head>
                                <body>
                                    {htmlBody}

                                    <div id='dynamic-quiz-container' class='quiz-container'></div>

                                    <script>
                                        window.currentQuiz = {quizJson};  
                                        {jsContent}
                                    </script>
                                    
                                </body>
                                </html>
                """;
    }

    private static async Task<string> LoadAssetAsString(string fileName)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            // Fallback or log error if file missing
            return "";
        }
    }
}