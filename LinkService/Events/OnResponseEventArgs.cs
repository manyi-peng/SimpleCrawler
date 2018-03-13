using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrawler.Events
{
    /// <summary>
    /// 爬虫完成事件
    /// </summary>
    public class OnResponseEventArgs
    {
        public long Milliseconds { get; private set; }// 爬虫请求执行时间
        public HttpStatusCode StatusCode { get; set; }//返回码


        public OnResponseEventArgs(HttpStatusCode StatusCode, long milliseconds)
        {
            this.StatusCode = StatusCode;
            this.Milliseconds = milliseconds;
        }
    }
}
