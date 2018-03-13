using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll
{
    public class MyException : Exception
    {
        public string bref { get; set; }

        public string message { get; set; }

    }
}
