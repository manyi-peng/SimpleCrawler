using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrawler.Events
{
    public class OnResponceErrorEventArgs
    {
        public Exception Exception { get; set; }

        public OnResponceErrorEventArgs(Exception exception) {
            this.Exception = exception;
        }
    }
}
