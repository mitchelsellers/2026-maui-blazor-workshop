# Calling Native/Mobile Features

Lets work wiht mobile specific stuff now!

## Add IDeviceContextService

Add to the mobile project services folder.

```` csharp
namespace FieldOps.Mobile.Services;

public interface IDeviceContextService
{
    bool IsConnected { get; }
    Task<string?> PickPhotoNameAsync();
    Task SaveLastViewedOrderAsync(Guid id);
    Task<Guid?> GetLastViewedOrderAsync();
}
````

## Add Implementation

Create a new file `MauiDeviceContextService.cs` within the services folder with the following

```` csharp
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

````

Then register this as a singleton within the program.cs

```` csharp
builder.Services.AddSingleton<IDeviceContextService, MauiDeviceContextService>();
````

## Create New Page to Demo

Create a new Page `DeviceTools.razor` with the following content

```` csharp
@page "/device"

@using FieldOps.Mobile.Services
@inject IDeviceContextService Device
@using Microsoft.Extensions.Logging
@inject ILogger<DeviceTools> Logger

<h1>Device capabilities</h1>

<p>Connectivity: <strong>@connection</strong></p>

<button class="btn btn-primary" @onclick="Refresh">Refresh connectivity</button>

<h2>Photo picker</h2>
<p>Choose an existing image; this lab displays its filename but does not upload or persist it.</p>

<button class="btn btn-primary" @onclick="PickAsync">Pick a photo</button>

@if (photo is not null)
{
	<p role="status">Selected: @photo</p>
}

@if (error is not null)
{
	<p class="error" role="alert">@error</p>
}

<h2>Local preference</h2>

<p>Last viewed work order: @(lastOrder?.ToString() ?? "none")</p>
<button class="btn" @onclick="LoadPreferenceAsync">Read saved preference</button>

@code {
	private string connection = "Unknown";
	private string? photo, error;
	private Guid? lastOrder;
	protected override async Task OnInitializedAsync()
	{
		Refresh();
		await LoadPreferenceAsync();
	}
	private void Refresh() => connection = Device.IsConnected ? "Internet access reported" : "No internet access reported";
	private async Task LoadPreferenceAsync() => lastOrder = await Device.GetLastViewedOrderAsync();
	private async Task PickAsync()
	{
		error = null;
		try
		{
			photo = await Device.PickPhotoNameAsync();
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Photo selection unavailable");
			error = "Photo selection was cancelled or is unavailable.";
		}
	}
}

````

## Add Item to Menu

Add a menu item to make it visible.

```` html
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="device">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Device
            </NavLink>
        </div>
````

## Update Work Order Details

We want to store the last-viewed item, so add an injected item at the top

```` csharp
@inject FieldOps.Mobile.Services.IDeviceContextService Device
````

Then after loading the item, add the following line

```` csharp
await Device.SaveLastViewedOrderAsync(Id);
````

## Validation

- Run the application go directly to the device page, see no perferences
- Pick a file
- Navigate to an item
- Go back

## Discussion Points

- Preferences are for non-sensitive small settings, not access tokens; use SecureStorage for secrets and consider platform backup behavior.
- Photo picker returns a reference to user-selected content; do not assume the file remains accessible indefinitely. Copy to app storage when needed and handle stream disposal.
- Media picker support and permissions vary by OS and version. Test on real devices.
- Handle user cancellation distinctly from unexpected errors in production.

# Bonus Items

As a bonus, we can also do Device Interactions

## Interface & Implementation

```` csharp
public interface IDeviceInteractions
{
    bool VibrationSupported();
    void VibrateDevice();
    bool CanGetPowerSource();
    string GetPowerSource();
    Task<string> GetLastKnownLocationAsync();
}
````

```` csharp
public class MauiDeviceInteractions : IDeviceInteractions
{
    public bool VibrationSupported()
    {
        //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/vibrate?view=net-maui-8.0&tabs=windows
        return Vibration.Default.IsSupported;
    }

    public void VibrateDevice()
    {
        if (VibrationSupported())
            Vibration.Default.Vibrate(TimeSpan.FromSeconds(3));
        else
            throw new NotSupportedException("Vibration is not supported on this device.");
    }

    public bool CanGetPowerSource()
    {
        return true;
    }

    public string GetPowerSource()
    {
        //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/battery?view=net-maui-8.0&tabs=windows
        return Battery.Default.PowerSource switch
        {
            BatteryPowerSource.Wireless => "Wireless charging",
            BatteryPowerSource.Usb => "USB cable charging",
            BatteryPowerSource.AC => "Device is plugged in to a power source",
            BatteryPowerSource.Battery => "Device isn't charging",
            _ => "Unknown"
        };
    }

    public async Task<string> GetLastKnownLocationAsync()
    {
        // Documentation: https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/geolocation?view=net-maui-8.0&tabs=android
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();

            if (location != null)
                return $"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}";
        }
        catch (FeatureNotSupportedException fnsEx)
        {
            // Handle not supported on device exception
            return "Not Supported";
        }
        catch (FeatureNotEnabledException fneEx)
        {
            // Handle not enabled on device exception
            return "Not Enabled";
        }
        catch (PermissionException pEx)
        {
            // Handle permission exception
            return "No Permission";
        }
        catch (Exception ex)
        {
            // Unable to get location
            return $"Error: {ex.Message}";
        }

        return "Unknown";
    }
}
````

## UI & Menu

New UI Page `DeviceAccess.razor`

```` csharp
@page "/DeviceAccess"

@using FieldOps.Mobile.Services

@inject IDeviceInteractions DeviceInteractions
@inject IJSRuntime JSRuntime

<PageTitle>Device Features</PageTitle>

<h4>Device Specific Features</h4>

<p>
    <button class="btn btn-primary" @onclick="TryVibrateDevice" disabled="@(!DeviceInteractions.VibrationSupported())">
        Try Vibrate Device
    </button>
</p>

<p>
    <button class="btn btn-primary" @onclick="TryCachedLocation">Try Cached Location (Last Known: @LastKnownLocation)</button>
</p>

<p>
    <button class="btn btn-primary" @onclick="GetPowerSource" disabled="@(!DeviceInteractions.CanGetPowerSource())">
        Get Power Source Information: @PowerSource)
    </button>
</p>

@code {

    public string LastKnownLocation { get; set; } = null!;

    public string PowerSource { get; set; } = "Not Checked";

    private async Task TryVibrateDevice()
    {
        if (DeviceInteractions.VibrationSupported())
        {
            DeviceInteractions.VibrateDevice();
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("alert", "Vibration is not supported");
        }
    }

    private async Task TryCachedLocation()
    {
        LastKnownLocation = await DeviceInteractions.GetLastKnownLocationAsync();
    }

    private void GetPowerSource()
    {
        PowerSource = DeviceInteractions.GetPowerSource();
    }
}
````

```` csharp
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="device">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Device
            </NavLink>
        </div>
````