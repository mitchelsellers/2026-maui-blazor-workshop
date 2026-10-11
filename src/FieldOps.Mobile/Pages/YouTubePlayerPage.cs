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