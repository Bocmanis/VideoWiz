using System;
using System.Collections.Generic;
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
    public sealed partial class FolderView : Page
    {
        public Directory Directory { get; set; }

        public FolderView()
        {
            this.InitializeComponent();

        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Directory = (Directory)e.Parameter;
            folderNameTextBox.Text = Directory.Name;
            SetSources();
        }

        private void SetSources()
        {
            folderListView.ItemsSource = Directory.Directories.Select(x => GetVideoListItem(x));
            itemListView.ItemsSource = Directory.Videos.Select(x => GetVideoListItem(x));
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

        private void FolderNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Directory.Name = textBox.Text;
                Cache.SaveAllEntriesToFile();
            }
        }
    }
}
