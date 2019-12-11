using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoWiz.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoWiz.Logic
{
    public class ItemAddingLogic
    {
        private StringBuilder LogMessage { get; set; }
        public async Task<string> AddItems(bool series = false, bool singleVideo = false)
        {
            LogMessage = new StringBuilder();
            var chosenFolder = await GetFolder();
            if (chosenFolder == null)
            {
                return "Canceled folder selection.";
            }
            LogMessage.AppendLine($"Folder: {chosenFolder.Path}");
            var mainFolder = new Directory()
            {
                Name = chosenFolder.Name,
                Parent = null,
                Path = chosenFolder.Path,
                Id = Guid.NewGuid(),
            };

            await GatherSubFolderAndFiles(chosenFolder, mainFolder, singleVideo);
            if (series)
            {
                Cache.AllEntries.Insert(0, mainFolder);
            }
            else
            {
                Cache.AllEntries.InsertRange(0, GetFolderWithVideosOnly(mainFolder));
            }
            Cache.SaveAllEntriesToFile();
            await Cache.InitThumbnails();
            await Cache.SetThumbnails();
            LogMessage.Append("Operation Successful!");
            return LogMessage.ToString();
        }

        private IEnumerable<Directory> GetFolderWithVideosOnly(Directory directory)
        {
            var result = new List<Directory>();
            var directories = directory.Directories.ToList();
            if (directory.Videos.Any())
            {
                directory.Directories = new List<Directory>();
                result.Add(directory);
            }

            foreach (var dir in directories)
            {
                result.AddRange(GetFolderWithVideosOnly(dir));
            }
            return result;
        }

        private async Task GatherSubFolderAndFiles(StorageFolder folder, Directory parent, bool singleVideo = false)
        {
            LogMessage.AppendLine($"***********************");
            LogMessage.AppendLine($"Folder: {folder.Name}");

            bool folderHasImage = false;
            foreach (var file in await folder.GetFilesAsync())
            {
                if (ImageExtensions.Contains(file.FileType.ToUpper()) && !folderHasImage)
                {
                    var copy = await file.CopyAsync(await Cache.GetThumbnailFolder());
                    await copy.RenameAsync(Cache.GetName(parent.Id.Value));
                    folderHasImage = true;
                }

                if (AllowedExtension.Contains(file.FileType.ToUpper()))
                {
                    LogMessage.AppendLine($"    {file.Name}");
                    VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    var title = videoProperties.Title;
                    var fileName = string.IsNullOrEmpty(title) ? file.Name : title;
                    var video = new Video()
                    {
                        Name = file.DisplayName,
                        Parent = parent,
                        Path = file.Path,
                        FileName = fileName,
                        ParentDirId = parent.Id.Value,
                        Id = Guid.NewGuid(),
                        Markers = new List<Markers>(),
                    };

                    var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView);
                    using (var reader = new DataReader(thumbnail.GetInputStreamAt(0)))
                    {
                        await reader.LoadAsync((uint)thumbnail.Size);
                        var buffer = new byte[(int)thumbnail.Size];
                        reader.ReadBytes(buffer);
                        Cache.SaveThumbnail(buffer, video.Id.Value);
                    }
                    parent.Videos.Add(video);

                    if (singleVideo)
                    {
                        return;
                    }
                }
            }

            foreach (var subFolder in await folder.GetFoldersAsync())
            {
                if (subFolder.Name.ToLower() == "sample" || subFolder.Name.ToLower() == "subtitles" || subFolder.Name.ToLower() == "subs")   
                {
                    continue;
                }
                var directory = new Directory()
                {
                    Name = subFolder.Name,
                    Parent = parent,
                    Path = subFolder.Path,
                    Id = Guid.NewGuid(),
                    ParentDirId = parent.Id.Value
                };
                parent.Directories.Add(directory);
                await GatherSubFolderAndFiles(subFolder, directory);
            }
        }
        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }
        }
                private async Task<StorageFolder> GetFolder()
        {
            var videoLibrary = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            var folder = await videoLibrary.RequestAddFolderAsync();

            //var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            //folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            //folderPicker.FileTypeFilter.Add("*");

            //StorageFolder folder2 = await folderPicker.PickSingleFolderAsync();

            return folder;
        }
        private static List<string> ImageExtensions => new List<string>()
        {
            ".JPG",
            ".PNG",
            ".GIF",
            ".TIF",
        };

        private static List<string> AllowedExtension => new List<string>()
        {
            ".PRPROJ",
            ".AVI",
            ".AEP",
            ".PSV",
            ".SWF",
            ".MKV",
            ".SFD",
            ".MP4",
            ".META",
            ".PIV",
            ".BIK",
            ".RMVB",
            ".WEBM",
            ".VEG",
            ".AEGRAPHIC",
            ".CPVC",
            ".DMX",
            ".IZZ",
            ".JTV",
            ".KDENLIVE",
            ".M1V",
            ".NCOR",
            ".SFVIDCAP",
            ".VIV",
            ".PZ",
            ".MSWMM",
            ".MXF",
            ".WLMP",
        };

    }
}
