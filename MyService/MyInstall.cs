using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MyService
{
    [RunInstaller(true)]
    public class MyInstall : Installer
    {
        ServiceProcessInstaller processInstall;
        ServiceInstaller serviceInstall;

        public MyInstall()
        {
            this.processInstall = new ServiceProcessInstaller();
            this.serviceInstall = new ServiceInstaller();
            processInstall.Account = ServiceAccount.LocalSystem;
            serviceInstall.StartType = ServiceStartMode.Automatic;
            this.serviceInstall.ServiceName = "数据抓取服务";
            this.Installers.Add(this.serviceInstall);
            this.Installers.Add(this.processInstall);
        }
    }
}
