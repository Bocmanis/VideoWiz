using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VideoWiz.Controls;
using VideoWiz.Logic;
using VideoWiz.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class HomePage : Page
    {
        public List<Video> Videos { get; set; }
        public HomePage()
        {
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetSources();
            Cache.SaveAllEntriesToFile();
            base.OnNavigatedTo(e);
        }

        private void SetSources()
        {
            continueWatchingListView.ItemsSource = null;
            collectionListView.ItemsSource = null;

            var videoItems = Cache.AllEntries.Select(x => GetVideoListItem(x));
            var continueWatching = Cache.GetResumeWatchingList.Select(x => GetVideoListItem(x, true));
            continueWatchingListView.ItemsSource = continueWatching;
            collectionListView.ItemsSource = videoItems;
        }

        private VideoListItem GetVideoListItem(Models.BaseUnit x, bool isResumeWatching = false)
        {
            var result = new VideoListItem(x, this, isResumeWatching);

            result.RemoveButtonClicked += Result_RemoveButtonClicked;

            return result;
        }

        private void Result_RemoveButtonClicked(object sender, EventArgs e)
        {
            SetSources();
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
                this.Frame.Navigate(typeof(VideoPlayer));
        }
    }
}
