# 009 - Authentication

Lets look at device specific stuff

## Storing Secure Tokens

Using `SecureStorage`

Create a new Interface, Implementation and DI Registration

```` csharp
namespace FieldOps.Mobile.Services;
public interface ISessionTokenStore
{
    Task SaveAsync(string accessToken);
    Task<string?> ReadAsync();
    Task ClearAsync();
}
public sealed class MauiSessionTokenStore : ISessionTokenStore
{
    private const string Key = "fieldops.demo.access-token";
    public Task SaveAsync(string accessToken) => SecureStorage.Default.SetAsync(Key, accessToken);
    public Task<string?> ReadAsync() => SecureStorage.Default.GetAsync(Key);
    public Task ClearAsync() { SecureStorage.Default.Remove(Key); return Task.CompletedTask; }
}
````

Add the DI Registration

```` csharp
builder.Services.AddSingleton<ISessionTokenStore, MauiSessionTokenStore>();
````

## Create a Bearer Token Handler to pass it alon

```` csharp
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
````

and DI Registration

```` csharp
builder.Services.AddTransient<BearerTokenHandler>();
````

Lastly, add to the HttpClient code, with the new HttpClient registration:

```` csharp
        builder.Services.AddHttpClient<ApiWorkOrderService>(client =>
        {
            client.BaseAddress = new Uri(ApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddHttpMessageHandler<BearerTokenHandler>();
````

## Create a UI to demo the behaviors

### Security.razor

```` csharp
@page "/security"

@inject FieldOps.Mobile.Services.ISessionTokenStore Tokens

<h1>Authentication architecture</h1>

<p>This lab demonstrates protected credential storage and request handling. It does not provide a sign-in flow or authenticate a user.</p>

<p>Token stored on device: <strong>@(stored ? "Yes" : "No")</strong></p>

<button class="btn btn-primary" @onclick="Clear">Clear stored token</button>

<button class="btn btn-danger" @onclick="GenerateRandom">Generate Random Token</button>

@if (error is not null)
{
	<p role="alert" class="error">@error</p>
}

@code {
	private bool stored;
	private string? error;
	protected override async Task OnInitializedAsync() => await Refresh();
	private async Task Refresh()
	{
		try
		{
			stored = !string.IsNullOrEmpty(await Tokens.ReadAsync());
		}
		catch (Exception)
		{
			error = "Secure storage is unavailable on this device.";
		}
	}

	private async Task Clear()
	{
		try
		{
			await Tokens.ClearAsync();
			await Refresh();
		}
		catch (Exception)
		{
			error = "Unable to clear secure storage.";
		}
	}

	private async Task GenerateRandom()
	{
		try
		{
			var token = Guid.NewGuid().ToString();
			await Tokens.SaveAsync(token);
			await Refresh();
		}
		catch (Exception)
		{
			error = "Unable to write to secure storage.";
		}
	}
}
````

Added menu item

```` csharp

````


# Discussion Points

- Is this proper
- What next?