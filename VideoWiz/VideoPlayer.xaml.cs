using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using VideoWiz.Logic;
using VideoWiz.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VideoWiz
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoPlayer : Page
    {
        public Video Video { get; set; }
        public bool ShowMarkers { get; private set; }

        public VideoPlayer()
        {
            this.InitializeComponent();
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.Interval = 5000;
            aTimer.Enabled = true;
        }

        private async void ATimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AddToResumeWatching();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {

                if (errorTextBlock.Dispatcher.HasThreadAccess)
                {
                    errorTextBlock.Text = string.Empty;
                }
            });
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer == null)
            {
                return;
            }

            PlayPause();
        }

        private void PlayPause()
        {
            if (mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
            {
                mediaPlayer.MediaPlayer.Pause();
            }
            else
            {
                mediaPlayer.MediaPlayer.Play();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter == null)
            {
                return;
            }
            
            Video = (Video)e.Parameter;
            InitializeVideoPlayer();
            markersDataGrid.ItemsSource = Video.Markers;
            this.ShowMarkers = true;
            ShowHideMarkers();
            this.SizeChanged += VideoPlayer_SizeChanged;
        }

        private void VideoPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowHideMarkers();
        }

        private async void InitializeVideoPlayer()
        {
            if (mediaPlayer.MediaPlayer == null)
            {
                mediaPlayer.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
            }
            mediaPlayer.MediaPlayer.RealTimePlayback = true;
            await SetMediaSource();
            mediaPlayer.AreTransportControlsEnabled = true;

            if (Video.IsStarted)
            {
                ResumeWatching();
            }
            else
            {
                mediaPlayer.MediaPlayer.Play();
            }
            mediaPlayer.MediaPlayer.Volume = Cache.Settings.Volume;

            mediaPlayer.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.DoubleTapped += MediaPlayer_DoubleTapped;
            mediaPlayer.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
            mediaPlayer.MediaPlayer.RealTimePlayback = true;
            mediaPlayer.MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            mediaPlayer.MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.MediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            mediaPlayer.MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        }

        private async void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            await SetNextVideo();
        }

        private async void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
           await SetPreviousVideo();
        }

        private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        {
            var volume = sender.Volume;
            Cache.Settings.Volume = volume;
            Cache.SaveSettingsToFile();
        }

        private void MediaPlayer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            mediaPlayer.IsFullWindow = !mediaPlayer.IsFullWindow;
        }

        private async System.Threading.Tasks.Task SetMediaSource()
        {
            //var folderPath = Path.GetDirectoryName(Video.Path);
            var videoFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(Video.Path);
            //var videoFile = await folder.GetFileAsync(Video.FileName);
            if (Video.Markers == null)
            {
                Video.Markers = new List<Markers>();
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        nameLabel.Text = Video.Name;
                        mediaPlayer.MediaPlayer.Source = MediaSource.CreateFromStorageFile(videoFile);
                        mediaPlayer.MediaPlayer.PlaybackMediaMarkerReached += MediaPlayer_PlaybackMediaMarkerReached;
                        SetMarkers();
                    }
                    ).AsTask();
        }

        private void SetMarkers()
        {
            mediaPlayer.MediaPlayer.PlaybackMediaMarkers.Clear();
            var skips = Video.Markers.Where(x => x.Action == ActionType.Skip);
            skips = skips.OrderBy(x => x.StartPosition);
            skips = MergeOVerlaying(skips.ToList());

            foreach (var marker in skips)
            {
                if (marker.TillEnd)
                {
                    TimeSpan duration = GetNaturalDuration();

                    var markerStart = duration.Subtract(marker.TillEndDuration);
                    var isBigger = markerStart > new TimeSpan(0);
                    if (isBigger)
                    {
                        var playbackMarker = new PlaybackMediaMarker(markerStart, ActionType.Skip.ToString(), mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration.ToString());
                        mediaPlayer.MediaPlayer.PlaybackMediaMarkers.Insert(playbackMarker);
                    }
                }
                else
                {
                    var playbackMarker = new PlaybackMediaMarker(marker.StartPosition, ActionType.Skip.ToString(), marker.EndPosition.ToString());
                    mediaPlayer.MediaPlayer.PlaybackMediaMarkers.Insert(playbackMarker);
                }
            }
        }

        public async void Pause()
        {
            if (mediaPlayer?.MediaPlayer == null)
            {
                return;
            }
            mediaPlayer.MediaPlayer.Pause();
        }

        private TimeSpan GetNaturalDuration()
        {
            var result = mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration;
            if (result.Ticks == 0)
            {
                Thread.Sleep(30);
                return GetNaturalDuration();
            }
            return result;
        }

        private IEnumerable<Markers> MergeOVerlaying(IEnumerable<Markers> skips)
        {
            foreach (var skip in skips)
            {
                var overLaying = skips.Where(x => x.StartPosition <= skip.EndPosition && !(x.StartPosition < skip.StartPosition) && x != skip);
                if (overLaying.Any())
                {
                    var newEndPoint = overLaying.Max(x => x.EndPosition);
                    var newMarker = new Markers()
                    {
                        Action = skip.Action,
                        StartPosition = skip.StartPosition,
                        EndPosition = newEndPoint,
                    };
                    var result = skips.ToList();
                    result.RemoveAll(x => x.StartPosition <= skip.EndPosition && !(x.StartPosition < skip.StartPosition));
                    result.Add(newMarker);
                    return MergeOVerlaying(result);
                }
            }
            return skips;
        }

        private void MediaPlayer_PlaybackMediaMarkerReached(MediaPlayer sender, PlaybackMediaMarkerReachedEventArgs args)
        {
            sender.PlaybackSession.Position = TimeSpan.Parse(args.PlaybackMediaMarker.Text);
        }

        private async void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            await SetNextVideo();
        }

        private async System.Threading.Tasks.Task SetNewVideo(Video nextVideo)
        {
            if (nextVideo != null)
            {
                Video = nextVideo;

                await SetMediaSource();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            mediaPlayer.MediaPlayer.Play();
                        }
                        ).AsTask();
            }
        }

        private Video GetNextVideo()
        {
            Models.Directory parent = GetVideoParent();
            Cache.ResumeWatching.RemoveAll(x => x.VideoId == Video.Id);
            var result = parent.Videos.Next(x => x.Id == Video.Id);
            if (result == null)
            {
                var parentsParent = Cache.AllDirectories.FirstOrDefault(x => x.Id == parent.ParentDirId);
                if (parentsParent != null)
                {
                    var nextSeason = parentsParent.Directories.Next(x => x.Id == parent.Id);
                    result = nextSeason.Videos.FirstOrDefault();
                }
            }
            return result;
        }

        private Video GetPreviousVideo()
        {
            Models.Directory parent = GetVideoParent();
            Cache.ResumeWatching.RemoveAll(x => x.VideoId == Video.Id);
            var result = parent.Videos.Previous(Video);
            if (result == null)
            {
                var parentsParent = Cache.AllDirectories.FirstOrDefault(x => x.Id == parent.ParentDirId);
                if (parentsParent != null)
                {
                    var nextSeason = parentsParent.Directories.Previous(parent);
                    result = nextSeason.Videos.FirstOrDefault();
                }
            }
            return result;
        }

        private Models.Directory GetVideoParent()
        {
            if (Video.Parent != null)
            {
                return Video.Parent;
            }
            return Cache.AllDirectories.FirstOrDefault(x => x.Id == Video.ParentDirId);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (mediaPlayer.MediaPlayer == null)
            {
                return;
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (mediaPlayer.MediaPlayer == null)
            {
                return;
            }
            AddToResumeWatching();
            Video.IsStarted = true;

            Cache.SaveResumeWatchingToFile();
        }

        private async void AddToResumeWatching()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (mediaPlayer?.MediaPlayer == null || mediaPlayer.MediaPlayer.CurrentState == MediaPlayerState.Closed)
                {
                    return;
                }
                Cache.ResumeWatching.RemoveAll(x => x.VideoId == Video.Id);
                var resumeWatching = new ResumeWatching()
                {
                    VideoId = Video.Id.Value,
                    Position = mediaPlayer.MediaPlayer.PlaybackSession.Position,
                };
                Cache.ResumeWatching.Insert(0, resumeWatching);
                Cache.SaveResumeWatchingToFile();
            }
            ).AsTask();
        }

        private void ResumeWatching()
        {
            mediaPlayer.MediaPlayer.PlaybackSession.Position = Video.Position;
            mediaPlayer.MediaPlayer.Play();
        }

        private void BackBigButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer != null)
            {
                mediaPlayer.MediaPlayer.PlaybackSession.Position = mediaPlayer.MediaPlayer.PlaybackSession.Position.Subtract(new TimeSpan(0, 0, 10));
            }
        }

        private void BackSmallButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer != null)
            {
                mediaPlayer.MediaPlayer.PlaybackSession.Position = mediaPlayer.MediaPlayer.PlaybackSession.Position.Subtract(new TimeSpan(0, 0, 0, 0, 500));
            }
        }

        private void ForwardSmallButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer != null)
            {
                mediaPlayer.MediaPlayer.PlaybackSession.Position = mediaPlayer.MediaPlayer.PlaybackSession.Position.Add(new TimeSpan(0, 0, 0, 0, 500));
            }
        }

        private void ForwardBigButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.MediaPlayer != null)
            {
                mediaPlayer.MediaPlayer.PlaybackSession.Position = mediaPlayer.MediaPlayer.PlaybackSession.Position.Add(new TimeSpan(0, 0, 10));
            }
        }

        private void FromStartCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!fromStartCheckBox.IsChecked ?? false)
            {
                fromTextBox.Text = string.Empty;
                fromTextBox.IsReadOnly = true;
            }
            else
            {
                fromTextBox.Text = new TimeSpan(0, 0, 0).ToString();
                fromTextBox.IsReadOnly = false;
            }
        }

        private void TillEndCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!tillEndCheckBox.IsChecked ?? false)
            {
                tillTextBox.Text = string.Empty;
                tillTextBox.IsReadOnly = true;
            }
            else
            {
                tillTextBox.Text = mediaPlayer.MediaPlayer.PlaybackSession.NaturalDuration.ToString();
                tillTextBox.IsReadOnly = false;
            }
        }

        private void AddMarkerButton_Click(object sender, RoutedEventArgs e)
        {
            errorTextBlock.Text = string.Empty;

            if (!TimeSpan.TryParse(fromTextBox.Text, out TimeSpan fromPoint))
            {
                fromTextBox.Text = "HH:mm:ss.fff";
                errorTextBlock.Text = "I like turtles, but smth is wrong with you";
                return;
            }
            if (!TimeSpan.TryParse(tillTextBox.Text, out TimeSpan tillPoint))
            {
                tillTextBox.Text = "HH:mm:ss.fff";
                errorTextBlock.Text = "Ayy, just click the button with . on it";
                return;
            }
            if (fromPoint > tillPoint)
            {
                errorTextBlock.Text = "Come on, you can do this. Start point has to be before end point";
                return;
            }
            var marker = new Markers()
            {
                Action = ActionType.Skip,
                StartPosition = fromPoint,
                EndPosition = tillPoint,
            };
            if (tillEndCheckBox.IsChecked ?? false)
            {
                marker.TillEnd = true;
                marker.TillEndDuration = marker.EndPosition.Subtract(marker.StartPosition);
            }

            Video.Markers.Add(marker);
            markersDataGrid.ItemsSource = null;
            markersDataGrid.ItemsSource = Video.Markers;
            SetMarkers();
        }

        private void FromTakeCurrent_Click(object sender, RoutedEventArgs e)
        {
            fromStartCheckBox.IsChecked = false;
            fromTextBox.Text = mediaPlayer.MediaPlayer.PlaybackSession.Position.ToString();
        }

        private void TilleTakeCurrent_Click(object sender, RoutedEventArgs e)
        {
            tillEndCheckBox.IsChecked = false;
            tillTextBox.Text = mediaPlayer.MediaPlayer.PlaybackSession.Position.ToString();
        }

        private void DeleteFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton)
            {
                Video.Markers.Remove((Markers)deleteButton.DataContext);
                markersDataGrid.ItemsSource = null;
                markersDataGrid.ItemsSource = Video.Markers;
            }
        }

        private void PlayMarker_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button playMarker)
            {
                var marker = (Markers)playMarker.DataContext;
                mediaPlayer.MediaPlayer.PlaybackMediaMarkers.Clear();
                var endPlaybackMarker = new PlaybackMediaMarker(marker.EndPosition, ActionType.RepeatSingle.ToString(), marker.StartPosition.ToString());

                mediaPlayer.MediaPlayer.PlaybackMediaMarkers.Insert(endPlaybackMarker);
                mediaPlayer.MediaPlayer.PlaybackSession.Position = marker.StartPosition;
                mediaPlayer.MediaPlayer.Play();
                resetRepeatButton.Visibility = Visibility.Visible;
            }
        }

        private void ApplyToAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button applyToAllButton)
            {
                var marker = (Markers)applyToAllButton.DataContext;
                var parent = GetVideoParent();
                foreach (var video in parent.Videos.Where(x => x.Id != Video.Id))
                {
                    video.Markers.Add(marker);
                }
                Cache.SaveAllEntriesToFile();
            }
        }

        private void NameLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Video.Name = textBox.Text;
            }
        }

        private async void PreviousVideoButton_Click(object sender, RoutedEventArgs e)
        {
            await SetPreviousVideo();
        }

        private async System.Threading.Tasks.Task SetPreviousVideo()
        {
            Video nextVideo = GetPreviousVideo();
            if (nextVideo == null)
            {
                return;
            }
            await SetNewVideo(nextVideo);
        }

        private async void NextVideoButton_Click(object sender, RoutedEventArgs e)
        {
            await SetNextVideo();
        }

        private async System.Threading.Tasks.Task SetNextVideo()
        {
            Video nextVideo = GetNextVideo();

            await SetNewVideo(nextVideo);
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.IsFullWindow = true;
        }

        private void ShowHideMarkersButton_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMarkers = !this.ShowMarkers;
            ShowHideMarkers();
        }

        private void ShowHideMarkers()
        {
            if (this.ShowMarkers)
            {
                mediaPlayer.MaxHeight = 1048-100;
                mediaPlayer.MaxWidth = 1818-20;
                mediaPlayer.Width = this.Frame.ActualWidth -20;
                mediaPlayer.Height = this.Frame.ActualHeight-100;
            }
            else
            {
                mediaPlayer.Width = 480;
                mediaPlayer.Height = 270;
            }
        }

        private void ResetRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            SetMarkers();
            resetRepeatButton.Visibility = Visibility.Collapsed;
        }

        private void MediaPlayer_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Escape:
                    mediaPlayer.IsFullWindow = false;
                    break;
                case Windows.System.VirtualKey.Space:
                    PlayPause();
                    break;
                default:
                    break;
            }
        }
    }
}
