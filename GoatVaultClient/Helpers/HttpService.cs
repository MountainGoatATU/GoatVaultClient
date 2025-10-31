using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoatVaultClient.Helpers
{
    public class HttpService
    {
        private readonly HttpClient _client;

        public HttpService(HttpClient client)
        {
            _client = client;
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string url, object payload = null)
        {
            try
            {
                // Create the HTTP request based on the method and URL
                using var request = new HttpRequestMessage(method, url);

                // If there's a payload, serialize it to JSON and add it to the request body
                if (payload != null)
                {
                    string json;

                    // Don’t double serialize if payload is already a string
                    if (payload is string jsonString)
                        json = jsonString;
                    else
                        json = JsonSerializer.Serialize(payload);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await _client.SendAsync(request);

                // Check for HTTP error responses
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    // Throw an exception with details about the error
                    throw new HttpRequestException(
                        $"HTTP {(int)response.StatusCode} ({response.StatusCode}) error calling {url}: {errorBody}");
                }

                // Read the response content as a string
                var responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    // Deserialize the JSON response into the specified type T
                    return JsonSerializer.Deserialize<T>(
                        responseString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch (JsonException jsonEx)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize response from {url}. Raw content: {responseString}",
                        jsonEx
                    );
                }
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                // A TaskCanceledException without user cancellation => timeout
                throw new TimeoutException($"Request to {url} timed out after {_client.Timeout.TotalSeconds} seconds.", ex);
            }
            catch (HttpRequestException ex)
            {
                // Network or protocol errors
                throw new InvalidOperationException($"Error performing HTTP request to {url}.", ex);
            }
            catch (Exception ex)
            {
                // Unexpected errors
                throw new Exception($"Unexpected error while sending request to {url}.", ex);
            }
        }

        public Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
        public Task<T> PostAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Post, url, payload);
        public Task<T> PatchAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Put, url, payload);
    }
}