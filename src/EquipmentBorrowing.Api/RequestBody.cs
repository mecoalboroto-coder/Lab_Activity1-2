using System.Text;
using System.Text.Json;

namespace EquipmentBorrowing.Api;

internal static class RequestBody
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<(T? Value, IResult? Error)> ReadAsync<T>(
        HttpRequest request,
        CancellationToken cancellationToken,
        bool allowEmpty = false) where T : class
    {
        string text;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            return allowEmpty
                ? (null, null)
                : (null, Invalid("The request body is required and must be a JSON object."));
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(text, Options);
            return value is null
                ? (null, Invalid("The request body must be a JSON object."))
                : (value, null);
        }
        catch (JsonException ex)
        {
            var message = ex.Message.Contains("could not be mapped", StringComparison.Ordinal)
                ? "The request contains a field that is not part of the contract or is not editable (identifiers, availability and status cannot be set)."
                : "The request body is not valid JSON for this endpoint. Check field names, types and date format (YYYY-MM-DD).";

            return (null, Invalid(message));
        }
    }

    private static IResult Invalid(string message) => ApiResults.Error(400, "invalid_request", message);
}
