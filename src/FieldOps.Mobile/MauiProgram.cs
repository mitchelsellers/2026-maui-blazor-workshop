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
        builder.Services.AddHttpClient<ApiWorkOrderService>(client =>
        {
            client.BaseAddress = new Uri(ApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddHttpMessageHandler<BearerTokenHandler>();

        //Add services for data, manually registering so we have all needed items
        builder.Services.AddSingleton<IWorkOrderService>(sp => sp.GetRequiredService<OfflineWorkOrderService>());
        builder.Services.AddSingleton<OfflineWorkOrderService>(sp => new OfflineWorkOrderService(sp.GetRequiredService<ApiWorkOrderService>(), sp.GetRequiredService<ILogger<OfflineWorkOrderService>>()));


        builder.Services.AddSingleton<IDeviceContextService, MauiDeviceContextService>();
        builder.Services.AddSingleton<IDeviceInteractions, MauiDeviceInteractions>();

        builder.Services.AddSingleton<ISessionTokenStore, MauiSessionTokenStore>();
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services.AddSingleton<WorkshopDiagnostics>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
