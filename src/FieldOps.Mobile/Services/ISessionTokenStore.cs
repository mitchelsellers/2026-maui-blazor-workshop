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