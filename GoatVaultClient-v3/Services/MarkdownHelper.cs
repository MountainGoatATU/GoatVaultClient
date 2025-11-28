using Markdig;

namespace YourNamespace
{
    public static class MarkdownHelper
    {
        public static async Task<string> GetHtmlFromAssetAsync(string filename)
        {
            // 1. Open the file from the Resources/Raw folder
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            using var reader = new StreamReader(stream);
            var markdownContent = await reader.ReadToEndAsync();

            // 2. Configure the pipeline (enable advanced features like tables)
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            // 3. Convert Markdown to HTML
            var htmlBody = Markdown.ToHtml(markdownContent, pipeline);

            // 4. Wrap it in a full HTML document with CSS styling
            return CreateHtmlDocument(htmlBody);
        }

        private static string CreateHtmlDocument(string htmlBody)
        {
            // Simple CSS for a clean "Documentation" look
            var css = @"
                <style>
                    body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; padding: 20px; line-height: 1.6; color: #333; }
                    h1 { color: #512BD4; border-bottom: 2px solid #eee; padding-bottom: 10px; }
                    h2 { color: #512BD4; margin-top: 30px; }
                    code { background-color: #f4f4f4; padding: 2px 5px; border-radius: 3px; font-family: Consolas, monospace; }
                    pre { background-color: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }
                    blockquote { border-left: 4px solid #512BD4; margin: 0; padding-left: 15px; color: #666; }
                    img { max-width: 100%; height: auto; }
                </style>";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    {css}
                </head>
                <body>
                    {htmlBody}
                </body>
                </html>";
        }
    }
}
