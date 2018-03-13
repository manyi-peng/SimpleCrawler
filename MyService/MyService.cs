using LinkService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleCrawler;

namespace MyService
{
    public partial class MyService : ServiceBase
    {
        Thread thread;

        public MyService()
        {
            InitializeComponent();

            this.ServiceName = "数据抓取服务";
        }

        protected override void OnStart(string[] args)
        {

            thread = new Thread(() =>
            {
                ServerResourceMonitor.ServerResource.Start();
                WeatherService.Start();
                InformationService.Start();
                LinkServiceValidate.Start();
            });

            thread.IsBackground = true;
            thread.Start();
        }

        protected override void OnStop()
        {
            if (this.thread != null)
            {
                if (this.thread.ThreadState == System.Threading.ThreadState.Running)
                {
                    this.thread.Abort();
                }
            }
        }
    }
}
