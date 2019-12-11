using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoWiz.Models
{
    public class Video : BaseUnit
    {
        public bool IsStarted { get; set; }
        public TimeSpan Position { get; set; }

        public List<Markers> Markers { get; set; }
    }
}
