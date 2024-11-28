using Microsoft.Extensions.Options;

namespace httpclient.ui.Handler;

public class AuthenticationOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class AuthenticationDelegatingHandler(IOptions<AuthenticationOptions> options)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("Authorization", options.Value.AccessToken);
        request.Headers.Add("User-Agent", options.Value.UserAgent);

        return base.SendAsync(request, cancellationToken);
    }
}