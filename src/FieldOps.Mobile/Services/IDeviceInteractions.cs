namespace FieldOps.Mobile.Services;

public interface IDeviceInteractions
{
    bool VibrationSupported();
    void VibrateDevice();
    bool CanGetPowerSource();
    string GetPowerSource();
    Task<string> GetLastKnownLocationAsync();
}