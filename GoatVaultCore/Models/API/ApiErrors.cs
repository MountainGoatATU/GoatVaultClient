using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class ValidationError
{
    [JsonPropertyName("loc")]
    public List<object> Loc { get; set; } = [];

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class HttpValidationError
{
    [JsonPropertyName("detail")]
    public List<ValidationError> Detail { get; set; } = [];
}

public class ApiException(string message, int statusCode, HttpValidationError? errors = null)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public HttpValidationError? Errors { get; } = errors;
}
