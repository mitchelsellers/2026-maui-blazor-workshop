namespace FieldOps.Mobile.Services;

public static class ApiSettings
{
    // Android emulator routes host machine traffic via 10.0.2.2. iOS simulator uses localhost.
    // Real devices require a reachable HTTPS endpoint and trusted certificate.
    public static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5088/" : "http://localhost:5088/";
}