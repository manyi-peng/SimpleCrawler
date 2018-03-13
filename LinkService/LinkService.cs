using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommonBll;
using SimpleCrawler.Events;
using Drision.Framework.Common;
using Drision.Framework.Entity.HighTechZone;
using CommonBll.Models;
using Drision.Framework.Linq;

namespace LinkService
{
    public class LinkService
    {

        public event EventHandler<OnResponseEventArgs> OnCompleted;//请求完成事件

        public event EventHandler<OnResponceErrorEventArgs> OnError;//请求出错事件

        public CookieContainer CookiesContainer { get; set; }//定义Cookie容器

        public LinkService() { }

        /// <summary>
        /// 异步创建请求
        /// </summary>
        /// <param name="uri">请求URL地址</param>
        /// <param name="proxy">代理服务器</param>
        /// <returns>网页源代码</returns>
        public async Task<string> BeginRequest(Uri uri, string proxy = null)
        {
            return await Task.Run(() =>
            {
                var pageSource = string.Empty;
                try
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    request.Accept = "*/*";
                    request.ServicePoint.Expect100Continue = false;//加快载入速度
                    request.ServicePoint.UseNagleAlgorithm = false;//禁止Nagle算法加快载入速度
                    request.AllowWriteStreamBuffering = false;//禁止缓冲加快载入速度
                    request.ContentType = "application/x-www-form-urlencoded";//定义文档类型及编码
                    request.AllowAutoRedirect = true;//禁止自动跳转
                    //设置User-Agent，伪装成Google Chrome浏览器
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
                    request.Timeout = 5000;//定义请求超时时间为5秒
                    request.KeepAlive = true;//启用长连接
                    request.Method = "GET";//定义请求方式为GET              
                    if (proxy != null) request.Proxy = new WebProxy(proxy);//设置代理服务器IP，伪装请求地址
                    request.ServicePoint.ConnectionLimit = int.MaxValue;//定义最大连接数
                    request.CookieContainer = this.CookiesContainer;//附加Cookie容器

                    using ( var response = (HttpWebResponse)request.GetResponse())
                    {//获取请求响应

                        foreach (Cookie cookie in response.Cookies) this.CookiesContainer.Add(cookie);//将Cookie加入容器，保存登录状态

                        OnCompleted?.Invoke(this, new OnResponseEventArgs(response.StatusCode, watch.ElapsedMilliseconds));
                    }
                    request.Abort();
                    watch.Stop();
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;//获取当前任务线程ID
                    var milliseconds = watch.ElapsedMilliseconds;//获取请求执行时间
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new OnResponceErrorEventArgs(ex));
                }
                return pageSource;
            });
        }
    }
}
