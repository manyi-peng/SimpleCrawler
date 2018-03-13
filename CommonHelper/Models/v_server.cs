using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drision.Framework.Entity.HighTechZone;

namespace CommonBll.Models
{
    public class v_server
    {
        public string name { get; set; }

        public int id { get; set; }

        public List<v_harddisk> disks { get; set; }

        public long totalmemory { get; set; }

        public long freememory { get; set; }

        public int cpu { get; set; }

        public string username { get; set; }

        public string password { get; set; }

        public string ip { get; set; }

        public v_server()
        {

        }

        public v_server(T_HTZ_Server p)
        {
            this.name = p.HTZ_Server_Name;
            this.id = p.HTZ_Server_Id;
            this.ip = p.ServerIp;
            this.username = p.LoginName;
            this.password = p.LoginPassword;
        }
    }
}
