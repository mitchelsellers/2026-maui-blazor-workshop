namespace FieldOps.Mobile.Services;

public interface IDeviceContextService
{
    bool IsConnected { get; }
    Task<string?> PickPhotoNameAsync();
    Task SaveLastViewedOrderAsync(Guid id);
    Task<Guid?> GetLastViewedOrderAsync();
}