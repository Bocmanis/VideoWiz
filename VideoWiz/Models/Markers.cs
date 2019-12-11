using System;

namespace VideoWiz.Models
{
    public class Markers
    {
        public TimeSpan StartPosition { get; set; }
        public TimeSpan EndPosition { get; set; }
        public string Description { get; set; }
        public ActionType Action { get; set; }
        public bool TillEnd { get; set; }
        public TimeSpan TillEndDuration { get; set; }
    }
}