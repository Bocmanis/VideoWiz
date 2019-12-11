using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoWiz.Models
{
    public class Directory : BaseUnit
    {
        public Directory()
        {
            Directories = new List<Directory>();
            Videos = new List<Video>();
        }
        public List<Directory> Directories  { get; set; }
        public List<Video> Videos { get; set; }
    }
}
