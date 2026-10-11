using System.Net.Http.Headers;

namespace FieldOps.Mobile.Services;

// Attaches a token acquired by a real OIDC client; this class does not acquire or refresh tokens.
public sealed class BearerTokenHandler(ISessionTokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.ReadAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}