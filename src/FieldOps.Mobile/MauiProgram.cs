using FieldOps.Core.Services;
using FieldOps.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FieldOps.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        //Make API useage needed
        builder.Services.AddHttpClient<IWorkOrderService, ApiWorkOrderService>(client =>
        {
            client.BaseAddress = new Uri(ApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
