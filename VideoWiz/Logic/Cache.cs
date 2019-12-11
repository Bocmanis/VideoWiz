using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoWiz.Models;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoWiz.Logic
{
    public static class Cache
    {
        private const string ThumbnailFolderName = "Thumbnails";
        private const string AllEntriesPath = "allEntries.json";
        private const string SettingsPath = "settings.json";
        private const string ResumeWatchingPath = "resumeWatching.json";
        public static string FolderPath { get; set; }

        private static List<Models.Directory> _allEntries;
        public static List<Models.Directory> AllEntries
        {
            get
            {
                if (_allEntries == null)
                {
                    _allEntries = LoadAllEntriesFromConfig();
                }
                return _allEntries;
            }
            set
            {
                _allEntries = value;
            }
        }

        public static List<Models.Directory> AllDirectories
        {
            get
            {
                var result = GetAllSubFolders(AllEntries);
                return result;
            }
        }

        private static List<Models.Directory> GetAllSubFolders(List<Models.Directory> directories)
        {
            var newDirs = directories.ToList();
            foreach (var dir in directories)
            {
                var subdirs = GetAllSubFolders(dir.Directories);
                newDirs.AddRange(subdirs);
            }
            return newDirs;
        }

        private static List<ResumeWatching> _resumeWatching;

        public static List<ResumeWatching> ResumeWatching
        {
            get
            {
                if (_resumeWatching == null)
                {
                    _resumeWatching = LoadResumeWatchingFromConfig();
                }
                return _resumeWatching;
            }
            set
            {
                _resumeWatching = value;
            }
        }

        public static List<StorageFile> Thumbnails { get; set; }


        public static IEnumerable<Video> GetResumeWatchingList
        {
            get
            {
                var result = new List<Video>();
                List<Video> allVideos = AllVideos;

                foreach (var resumeWatch in ResumeWatching)
                {
                    var video = allVideos.FirstOrDefault(x => x.Id == resumeWatch.VideoId);
                    if (video != null)
                    {
                        video.Position = resumeWatch.Position;
                        result.Add(video);
                    }
                }
                return result;
            }
        }

        public static List<Video> AllVideos
        {
            get
            {
                return AllDirectories.SelectMany(x => x.Videos).ToList();
            }
        }

        private static Settings _settings { get; set; }
        public static Settings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = LoadSettingsFromConfig();
                }
                return _settings;
            }
        }

        public static void SaveResumeWatchingToFile()
        {
            var jsonString = JsonConvert.SerializeObject(_resumeWatching, Formatting.Indented);
            var path = Path.Combine(FolderPath, ResumeWatchingPath);
            File.WriteAllText(path, jsonString);
        }

        internal static async Task<bool> SetThumbnails()
        {
            foreach (var video in Cache.AllVideos)
            {
                video.Image = await Cache.GetImage(video.Id.Value);
            }
            foreach (var directory in Cache.AllDirectories)
            {
                var image = await Cache.GetImage(directory.Id.Value);
                if (image == null)
                {
                    image = directory.Videos.FirstOrDefault(x => x.Image != null)?.Image;
                }
                directory.Image = image;
            }
            return true;
        }

        public static void SaveSettingsToFile()
        {
            var jsonString = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            var path = Path.Combine(FolderPath, SettingsPath);
            File.WriteAllText(path, jsonString);
        }

        private static List<ResumeWatching> LoadResumeWatchingFromConfig()
        {
            var fileName = ResumeWatchingPath;
            string jsonText = GetJsonFromFile(fileName);
            var result = JsonConvert.DeserializeObject<List<Models.ResumeWatching>>(jsonText) ?? new List<Models.ResumeWatching>();
            return result;
        }

        private static Settings LoadSettingsFromConfig()
        {
            var fileName = SettingsPath;
            string jsonText = GetJsonFromFile(fileName);
            var result = JsonConvert.DeserializeObject<Settings>(jsonText) ?? new Settings() { Volume = 100};
            return result;
        }

        public static void SaveAllEntriesToFile()
        {
            var jsonString = JsonConvert.SerializeObject(_allEntries, Formatting.Indented);
            var path = Path.Combine(FolderPath, AllEntriesPath);
            File.WriteAllText(path, jsonString);
        }

        private static List<Models.Directory> LoadAllEntriesFromConfig()
        {
            var fileName = AllEntriesPath;
            string jsonText = GetJsonFromFile(fileName);
            var result = JsonConvert.DeserializeObject<List<Models.Directory>>(jsonText) ?? new List<Models.Directory>();
            return result;
        }

        internal async static void SaveThumbnail(byte[] buffer, Guid videoId)
        {
            StorageFolder thumbnailFolder = await GetThumbnailFolder();
            var file = await thumbnailFolder.CreateFileAsync(GetName(videoId), CreationCollisionOption.OpenIfExists);
            var props = await file.GetBasicPropertiesAsync();
            if (props.Size == 0)
            {
                await Windows.Storage.FileIO.WriteBytesAsync(file, buffer);
            }
        }

        public static string GetName(Guid videoId)
        {
            return $"{videoId.ToString()}.jpeg";
        }

        public static async Task<StorageFolder> GetThumbnailFolder()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync(ThumbnailFolderName, CreationCollisionOption.OpenIfExists);
        }

        public static async Task<BitmapImage> GetImage(Guid videoId)
        {
            if (Thumbnails == null)
            {
                await InitThumbnails();
            }

            var videoName = videoId.ToString();
            var thumbnail = Thumbnails.FirstOrDefault(x => x.DisplayName == videoName);
            if (thumbnail == null)
            {
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.SetSource(await thumbnail.OpenAsync(FileAccessMode.Read));
            bitmap.DecodePixelHeight = 130;
            bitmap.DecodePixelWidth = 190;
            return bitmap;
        }

        public static async Task<bool> InitThumbnails()
        {
            var folder = await GetThumbnailFolder();
            var result = await folder.GetFilesAsync();
            Thumbnails = result.ToList();
            return true;
        }

        private static string GetJsonFromFile(string fileName)
        {
            string folderPath = FolderPath;
            var path = Path.Combine(folderPath, fileName);
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            var jsonText = File.ReadAllText(path);
            return jsonText;
        }
    }
}
