using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoWiz.Models
{
    public class ResumeWatching
    {
        public Guid VideoId { get; set; }
        public TimeSpan Position { get; set; }
    }
}
