namespace FieldOps.Mobile.Services;

public sealed class MauiDeviceContextService : IDeviceContextService
{
    private const string LastOrderKey = "last-viewed-order";
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public async Task<string?> PickPhotoNameAsync()
    {
        // Picker is intentionally invoked from a user gesture; no file is uploaded.
        var result = await MediaPicker.Default.PickPhotoAsync();
        return result?.FileName;
    }
    public Task SaveLastViewedOrderAsync(Guid id)
    {
        Preferences.Default.Set(LastOrderKey, id.ToString());
        return Task.CompletedTask;
    }
    public Task<Guid?> GetLastViewedOrderAsync()
    {
        var value = Preferences.Default.Get(LastOrderKey, string.Empty);
        return Task.FromResult(Guid.TryParse(value, out var id) ? (Guid?)id : null);
    }
}
