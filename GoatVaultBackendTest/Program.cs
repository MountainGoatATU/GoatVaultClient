using System.Text;
using System.Text.Json;

namespace GoatVaultBackendTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing GoatVault Backend...");



            string email = "testuser" + Guid.NewGuid() + "@example.com";
            string password = "MyPassword123!";

            var userPayload = new
            {
                email = email,
                password = password
            };

            string registerUrl = "http://127.0.0.1:8000/v1/users/";

            using var client = new HttpClient();

            // Optional: set User-Agent if backend checks it
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GoatVaultTest/1.0");

            try
            {
                // Convert payload to JSON
                string json = JsonSerializer.Serialize(userPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(registerUrl, content);

                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Registration successful!");
                    Console.WriteLine("Response: " + responseString);

                    // Parse JWT token
                    var jsonDoc = JsonDocument.Parse(responseString);
                    if (jsonDoc.RootElement.TryGetProperty("token", out var token))
                    {
                        Console.WriteLine("JWT Token: " + token.GetString());
                    }
                    else
                    {
                        Console.WriteLine("No token returned.");
                    }
                }
                else
                {
                    Console.WriteLine("Registration failed!");
                    Console.WriteLine("Status code: " + response.StatusCode);
                    Console.WriteLine("Response: " + responseString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
