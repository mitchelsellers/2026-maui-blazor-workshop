namespace FieldOps.Mobile.Services;

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