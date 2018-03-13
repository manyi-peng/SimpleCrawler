using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll
{
    public class MyCommonManager
    {
        public static void ErrorLog(Exception ex, string errortype, string rootdir)
        {
            string logdir = System.IO.Path.Combine(rootdir, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!System.IO.Directory.Exists(logdir))
            {
                System.IO.Directory.CreateDirectory(logdir);
            }
            string logfilename = System.IO.Path.Combine(logdir, "log.txt");
            System.IO.File.AppendAllText(logfilename, ex.Message + "\t" + errortype + "\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");
        }

    }
}
