# 011 - Video Player & Blazor

This is an example, from the real-world, of a situation where you may need a native control, to handle what should be considered 'easy' within Blazor.

Playing an embedded YouTube Video

## Setup a Custom Web View

Add this under `/Controls/YouTubeVideoView.cs`

```` csharp
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
````

## Create the CUstomer Player Page

THis should be added under `/Pages/YouTubePlayerPage.cs`

```` csharp
using FieldOps.Mobile.Controls;

namespace FieldOps.Mobile.Pages;

public sealed class YouTubePlayerPage : ContentPage
{
    private readonly YouTubeVideoView _player;
    private readonly Label _status;
    private Window? _playbackWindow;
    private bool _closing;

    public YouTubePlayerPage(string videoId)
    {
        Title = "Video";
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
        _player = new YouTubeVideoView(videoId);
        _status = new Label { Text = "Tap the video to play. If playback is unavailable, open it in YouTube." };
        _player.Navigated += (_, e) =>
        {
            if (e.Result != WebNavigationResult.Success)
                _status.Text = "Unable to load the player. Check your connection or open it in YouTube.";
        };

        var close = new Button { Text = "Close", HorizontalOptions = LayoutOptions.End };
        close.Clicked += async (_, _) =>
        {
            if (_closing) return;
            _closing = true;
            try
            {
                _player.StopVideo();
                await Navigation.PopModalAsync();
            }
            finally { _closing = false; }
        };
        var external = new Button { Text = "Open in YouTube" };
        external.Clicked += async (_, _) =>
        {
            _player.StopVideo();
            try
            {
                if (!await Launcher.Default.OpenAsync($"https://www.youtube.com/watch?v={videoId}"))
                {
                    _status.Text = "Unable to open YouTube. Please try again.";
                    _player.ResumeVideo();
                }
            }
            catch
            {
                _status.Text = "Unable to open YouTube. Please try again.";
                _player.ResumeVideo();
            }
        };
        var layout = new Grid
        {
            Padding = 16,
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)
            }
        };
        layout.Add(close, 0, 0);
        layout.Add(_player, 0, 1);
        layout.Add(_status, 0, 2);
        layout.Add(external, 0, 3);
        Content = layout;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _playbackWindow = Window;
        if (_playbackWindow is not null)
        {
            _playbackWindow.Stopped += OnWindowStopped;
            _playbackWindow.Resumed += OnWindowResumed;
        }
        _player.ResumeVideo();
    }

    protected override void OnDisappearing()
    {
        _player.StopVideo();
        if (_playbackWindow is not null)
        {
            _playbackWindow.Stopped -= OnWindowStopped;
            _playbackWindow.Resumed -= OnWindowResumed;
            _playbackWindow = null;
        }
        base.OnDisappearing();
    }

    private void OnWindowStopped(object? sender, EventArgs e) => _player.StopVideo();
    private void OnWindowResumed(object? sender, EventArgs e) => _player.ResumeVideo();
}
````

## Create YouTubePlayBackService & IMplementation

`/Services/IYouTubePlaybackService.cs`

```` csharp
namespace FieldOps.Mobile.Services;

public interface IYouTubePlaybackService
{
    Task ShowAsync(string videoId);
}

````

`/Services/YouTubePlaybackService.cs`

```` csharp
using FieldOps.Mobile.Pages;

namespace FieldOps.Mobile.Services;

public sealed class YouTubePlaybackService : IYouTubePlaybackService
{
    private bool _opening;

    public Task ShowAsync(string videoId) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var root = Application.Current?.Windows.FirstOrDefault()?.Page
            ?? throw new InvalidOperationException("No active application window.");
        if (_opening || root.Navigation.ModalStack.Any(page => page is YouTubePlayerPage))
            return;

        _opening = true;
        try
        {
            await root.Navigation.PushModalAsync(new YouTubePlayerPage(videoId));
        }
        finally { _opening = false; }
    });
}
````

Lastly add the DI registration to MauiProgram.cs

```` csharp
builder.Services.AddSingleton<IYouTubePlaybackService, YouTubePlaybackService>();
````

## Update the Details Page to show the video

Add the Injected service to the top

```` csharp
@inject FieldOps.Mobile.Services.IYouTubePlaybackService YouTubePlaybackService
````

Add the following to the actual details to trigger

```` csharp
<p><button class="btn btn-secondary" @onclick="ViewVideoAsync">View Video</button></p>
````

Lastly, add this method to handle the call, this is a sample video of mine

```` csharp
	private async Task ViewVideoAsync()
	{
		var videoId = "tplcuaHzpSQ";
		await YouTubePlaybackService.ShowAsync(videoId);
	}
````

# Discussion

- Why?
- Does this fully work?
- What if we have overhead that isn't needed?