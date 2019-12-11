using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoWiz.Models
{
    public class BaseUnit
    {
        public string Name { get; set; }
        [JsonIgnore]
        public Directory Parent { get; set; }
        public Guid ParentDirId { get; set; }
        public string Picture { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public Guid? Id { get; set; }
        public BitmapImage Image { get; set; }
    }
}
