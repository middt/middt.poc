using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace httpclient.ui.Handler;
public class ClientCredentialAuthenticationOptions
{
    public string? ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; } = string.Empty;
    public string? TokenUrl { get; set; } = string.Empty;
}

public class AuthToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int expires_in { get; set; }
    [JsonPropertyName("token_type")]
    public string token_type { get; set; }
    [JsonPropertyName("scope")]
    public string scope { get; set; }
}


public class OAuth2ClientCredentialAuthorizationDelegatingHandler
(IOptions<ClientCredentialAuthenticationOptions> clientCredentialAuthenticationOptions)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authToken = await GetAccessTokenAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            JwtBearerDefaults.AuthenticationScheme,
            authToken.AccessToken);

        var httpResponseMessage = await base.SendAsync(
            request,
            cancellationToken);

        httpResponseMessage.EnsureSuccessStatusCode();

        return httpResponseMessage;
    }

    private async Task<AuthToken> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("client_id", clientCredentialAuthenticationOptions.Value.ClientId),
            new("client_secret", clientCredentialAuthenticationOptions.Value.ClientSecret),
            new("scope", "openid email"),
            new("grant_type", "client_credentials")
        };

        var content = new FormUrlEncodedContent(parameters);

        var authRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(clientCredentialAuthenticationOptions.Value.TokenUrl))
        {
            Content = content
        };

        var response = await base.SendAsync(authRequest, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthToken>() ??
               throw new ApplicationException();
    }
}