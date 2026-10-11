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