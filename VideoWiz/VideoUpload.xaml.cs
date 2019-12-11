using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VideoWiz.Logic;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoWiz
{
    public sealed partial class VideoUpload : Page
    {
        public VideoUpload()
        {
            this.InitializeComponent();
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var log = await new ItemAddingLogic().AddItems();
            debugWindow.Text = log;
        }
        private async void SelectFolderSingleVideoButton_Click(object sender, RoutedEventArgs e)
        {
            var log = await new ItemAddingLogic().AddItems(false, true);
            debugWindow.Text = log;
        }
        
        private void GoToCollectionButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void SelectSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            var log = await new ItemAddingLogic().AddItems(true);
            debugWindow.Text = log;
        }
    }
}
