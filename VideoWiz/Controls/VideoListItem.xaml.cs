using VideoWiz.Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Linq;
using VideoWiz.Logic;
using System;
using System.Threading.Tasks;

namespace VideoWiz.Controls
{
    public partial class VideoListItem : UserControl
    {
        private readonly bool doResumeWatching;

        public BaseUnit BaseUnit { get; set; }
        public Page Page { get; set; }

        public VideoListItem(BaseUnit baseUnit, Page page, bool doResumeWatching = false)
        {
            this.BaseUnit = baseUnit;
            this.Page = page;
            this.doResumeWatching = doResumeWatching;
            this.InitializeComponent();

            this.VideoImage.Source = baseUnit.Image;
            this.VideoImage.UpdateLayout();
        }

        public event EventHandler RemoveButtonClicked;
        protected virtual void OnCloseButtonClicked(EventArgs e)
        {
            var handler = RemoveButtonClicked;
            if (handler != null)
                handler(this, e);
        }

        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (BaseUnit is Directory directory)
            {
                if (directory.Directories.Count == 0 && directory.Videos.Count == 1)
                {
                    var target = directory.Videos.FirstOrDefault();
                    NavigateToFrame(typeof(VideoPlayer), target);
                }
                else
                {
                    NavigateToFrame(typeof(FolderView), directory, true);
                }
            }
            else if (BaseUnit is Video video)
            {
                if (doResumeWatching)
                {
                    video.IsStarted = true;
                }
                else
                {
                    video.IsStarted = false;
                }
                NavigateToFrame(typeof(VideoPlayer), video);
            }
        }

        private async void NavigateToFrame(Type type, BaseUnit target, bool isDirectory = false)
        {
            if (await ValidatePath(target.Path, isDirectory))
            {
                Page.Frame.Navigate(type, target);
            }
        }

        private async Task<bool> ValidatePath(string path, bool isDirectory = false)
        {
            try
            {
                if (isDirectory)
                {
                }
                else
                {
                    var videoFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                }
                return true;
            }
            catch (Exception)
            {
                
                RemoveItem();
                return false;
            }
        }

        private void RemoveButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RemoveItem();

        }

        private void RemoveItem()
        {
            if (BaseUnit is Directory directory)
            {
                var parent = Cache.AllDirectories.FirstOrDefault(x => x.Id == directory.ParentDirId);
                if (parent != null && parent.Directories.Contains(directory))
                {
                    parent.Directories.Remove(directory);
                }
                else
                {
                    Cache.AllEntries.Remove(directory);
                }
            }
            else if (BaseUnit is Video video)
            {
                if (doResumeWatching)
                {
                    Cache.ResumeWatching.RemoveAll(x => x.VideoId == video.Id);
                }
                else
                {
                    var parent = Cache.AllDirectories.FirstOrDefault(x => x.Id == video.ParentDirId);
                    if (parent != null)
                    {
                        parent.Videos.Remove(video);
                    }
                }
            }

            OnCloseButtonClicked(null);
        }
    }
}
