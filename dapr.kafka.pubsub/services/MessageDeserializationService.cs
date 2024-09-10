using System.Text.Json;

public class MessageDeserializationService : IMessageDeserializationService
{
    private readonly JsonSerializerOptions _options;

    public MessageDeserializationService()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> DeserializeMessageAsync(Stream requestBody)
    {
        return await JsonSerializer.DeserializeAsync<string>(requestBody, _options)
            ?? throw new InvalidOperationException("Failed to deserialize message");
    }
}