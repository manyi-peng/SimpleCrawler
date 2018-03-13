using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll.Models
{
    public class v_harddisk
    {
        public string name { get; set; }

        public string FileSystem { get; set; }

        public long FreeSpace { get; set; }

        public long Size { get; set; }
    }
}
