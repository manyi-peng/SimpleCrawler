using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCrawler.Events
{
    /// <summary>
    /// 爬虫启动事件
    /// </summary>
    public class OnStartEventArgs
    {
        public Uri Uri { get; set; }// 爬虫URL地址
        public string Encode { get; set; }// 编码格式

        public OnStartEventArgs(Uri uri, string encode)
        {
            this.Uri = uri;
            this.Encode = encode;
        }
    }
}
