using System.Text.Json;

public interface IMessageDeserializationService
{
    Task<string> DeserializeMessageAsync(Stream requestBody);
}