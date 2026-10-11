using System.Text.RegularExpressions;
#if IOS
using Foundation;
using WebKit;
#endif

namespace FieldOps.Mobile.Controls;

/// <summary>A separate WebView so player identity never changes the Blazor host's origin.</summary>
public sealed class YouTubeVideoView : WebView
{
    private readonly string _videoId;
    private bool _stopped;

    public YouTubeVideoView(string videoId)
    {
        if (!Regex.IsMatch(videoId, "^[a-zA-Z0-9_-]{11}$"))
            throw new ArgumentException("Invalid YouTube video ID.", nameof(videoId));

        _videoId = videoId;
        MinimumHeightRequest = 200;
        BackgroundColor = Colors.Black;
    }

    public string ReferrerUrl => $"https://{AppInfo.Current.PackageName.ToLowerInvariant()}/";

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (!_stopped)
            LoadVideo();
    }

    public void ResumeVideo()
    {
        if (!_stopped)
            return;
        _stopped = false;
        LoadVideo();
    }

    public void StopVideo()
    {
        _stopped = true;
        // Unload the document, including media in cross-origin frames.
#if IOS
        if (Handler?.PlatformView is WKWebView webView)
        {
            webView.StopLoading();
            webView.LoadHtmlString("<html></html>", null);
        }
#elif ANDROID
        if (Handler?.PlatformView is Android.Webkit.WebView webView)
        {
            webView.StopLoading();
            webView.LoadUrl("about:blank");
        }
#else
        Source = new HtmlWebViewSource { Html = "<html></html>" };
#endif
    }

    private async Task LoadVideo()
    {
        var embedUrl = $"https://www.youtube.com/embed/{_videoId}?playsinline=1&rel=0";
        System.Diagnostics.Debug.WriteLine($"YouTube player: {embedUrl}; Referer: {ReferrerUrl}");
#if IOS
        // MAUI's native WebView enables inline media playback in its creation configuration.
        if (Handler?.PlatformView is not WKWebView webView)
            return;
        using var request = new NSMutableUrlRequest(new NSUrl(embedUrl));
        request.Headers = NSDictionary.FromObjectsAndKeys(
            new NSObject[] { new NSString(ReferrerUrl) },
            new NSObject[] { new NSString("Referer") });
        webView.LoadRequest(request);
#elif ANDROID
        if (Handler?.PlatformView is not Android.Webkit.WebView webView)
            return;
        var html = $$"""
            <!doctype html><html><head>
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="referrer" content="strict-origin-when-cross-origin">
            <style>html,body{margin:0;height:100%;background:black}iframe{width:100%;height:100%;border:0}</style>
            </head><body><iframe src="{{embedUrl}}" title="YouTube video player"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
            referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe></body></html>
            """;
        webView.LoadDataWithBaseURL(ReferrerUrl, html, "text/html", "UTF-8", null);
#elif WINDOWS
    if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView2)
    {
        await webView2.EnsureCoreWebView2Async();

        var request = webView2.CoreWebView2.Environment.CreateWebResourceRequest(
            embedUrl,
            "GET",
            null,
            $"Referer: {ReferrerUrl}");

        webView2.CoreWebView2.NavigateWithWebResourceRequest(request);
    }
#else
        Source = embedUrl;
#endif
    }
}