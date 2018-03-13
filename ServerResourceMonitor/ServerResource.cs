using CommonBll.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Drision.Framework.Entity.HighTechZone;
using Drision.Framework.Common;
using Drision.Framework.Linq;
using Drision.Framework.Common.EntityLibrary;
using CommonBll;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ServerResourceMonitor
{
    public class ServerResource
    {
        private const string PROPERTY_CAPACITY = "Capacity";
        private const string PROPERTY_AVAILABLE_BYTES = "AvailableBytes";

        public async static void Start()
        {
            while (true)
            {
                var waittime = 24 * 60 * 60 * 1000;
                try
                {

                    using (var db = new BizDataContext())
                    {
                        var temp = db.Set<T_Configuration>().Where(p => p.Configuration_Key == CommonHelper.SERVERKEY).FirstOrDefault();
                        if (temp != null)
                        {
                            waittime = Int32.Parse(temp.Configuration_Value);
                        }

                        var servers = db.Set<T_HTZ_Server>().Where(p => p.State == v_common.YesState).Select(p => new v_server(p));

                        foreach (var item in servers)
                        {
                            await SaveData(item);
                        }
                    }
                }
                catch (Exception e)
                {

                }
                finally
                {
                    Thread.Sleep(waittime);
                }
            }

        }

        public static async Task SaveData(v_server result)
        {
            ConnectionOptions Conn = new ConnectionOptions();

            if (!IsCurrent(result.ip))
            {
                //设定用于WMI连接操作的用户名   
                Conn.Username = result.username;
                //设定用户的口令
                Conn.Password = result.password;
            }
            var Ms = new ManagementScope("\\\\" + result.ip + "\\root\\cimv2", Conn);
            try
            {
                Ms.Connect();
            }
            catch (Exception ex)
            {
                using (var db = new BizDataContext())
                {
                    await SaveServerLog(db, result, (int)HTZ_ServiceState_ServiceStateEnum.Wrong);
                }
                Logs.WriteLog(ex);
                return;
            }
            var query1 = new SelectQuery("SELECT * FROM Win32_PhysicalMemory");
            var query2 = new SelectQuery("SELECT * FROM Win32_PerfRawData_PerfOS_Memory");
            var query3 = new SelectQuery("SELECT * FROM Win32_Processor");
            var query4 = new SelectQuery("SELECT * FROM Win32_LogicalDisk");

            var searcher1 = new ManagementObjectSearcher(Ms, query1);
            var searcher2 = new ManagementObjectSearcher(Ms, query2);
            var searcher3 = new ManagementObjectSearcher(Ms, query3);
            var searcher4 = new ManagementObjectSearcher(Ms, query4);

            var capacity = 0L;
            var free = 0L;
            var cpu = 0;

            var disks = new List<v_harddisk>();

            foreach (var o in searcher1.Get())
                capacity += long.Parse(o[PROPERTY_CAPACITY].ToString());
            foreach (var o in searcher2.Get())
                free += long.Parse(o[PROPERTY_AVAILABLE_BYTES].ToString());

            var cpuData = searcher3.Get();
            foreach (var o in cpuData)
            {
                if (o["LoadPercentage"] != null)
                {
                    cpu += int.Parse(o["LoadPercentage"].ToString());
                }
                else
                {
                    cpu = 0;
                }
            }

            cpu = cpu / cpuData.Count;


            foreach (var o in searcher4.Get())
            {
                var disk = new v_harddisk();
                if (o["FileSystem"] != null)
                {
                    disk.FileSystem = o["FileSystem"].ToString();
                }

                if (o["FreeSpace"] != null)
                {
                    disk.FreeSpace = long.Parse(o["FreeSpace"].ToString());
                }

                disk.name = o["Name"].ToString();

                if (o["FreeSpace"] != null)
                {
                    disk.Size = long.Parse(o["Size"].ToString());
                }

                if ("NTFS" == disk.FileSystem)
                {
                    disks.Add(disk);
                }
            }

            result.cpu = cpu;
            result.freememory = free;
            result.totalmemory = capacity;
            result.disks = disks;

            using (var db = new BizDataContext())
            {
                await SaveServerLog(db, result, (int)HTZ_ServiceState_ServiceStateEnum.Fine);
                await SaveServerDisks(db, result);
            }
        }

        private static bool IsCurrent(string ip)
        {
            bool flag = false;
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork && ipa.ToString() == ip)
                {
                    return true;
                }
            }
            return flag;
        }

        public static async Task SaveServerDisks(BizDataContext db, v_server server)
        {
            var dbdisks = await db.Set<T_HTZ_ServerHardDisk>().Where(p => p.State == v_common.YesState && p.HTZ_ServerId == server.id).ToListAsync();

            foreach (var item in server.disks)
            {
                var disk = dbdisks.Where(p => p.HTZ_ServerHardDisk_Name == item.name && p.State == v_common.YesState).FirstOrDefault();

                if (disk == null)
                {
                    disk = new T_HTZ_ServerHardDisk();
                    disk.HTZ_ServerHardDisk_Name = item.name;
                    disk.CreateTime = DateTime.Now;
                    disk.UpdateTime = DateTime.Now;
                    disk.TotalCapacity = (int)(item.Size / 1024 / 1024 / 1024);
                    disk.UserCapacity = (int)(item.FreeSpace / 1024 / 1024 / 1024);
                    disk.State = v_common.YesState;
                    disk.UpdateTime = disk.CreateTime;
                    disk.HTZ_ServerHardDisk_Id = await db.GetNextIdentity_IntAsync();
                    disk.HTZ_ServerId = server.id;

                    await db.InsertAsync(disk);
                }
                else
                {
                    disk.UpdateTime = DateTime.Now;
                    disk.TotalCapacity = (int)(item.Size / 1024 / 1024 / 1024);
                    disk.UserCapacity = (int)(item.FreeSpace / 1024 / 1024 / 1024);
                    await db.UpdatePartialAsync(disk, p => new { p.UpdateTime, p.TotalCapacity, p.UserCapacity });
                }
            }
        }

        private static async Task SaveServerLog(BizDataContext db, v_server server, int serverstate)
        {
            var log = new T_HTZ_ServerLog();
            log.CpuOccupancy = server.cpu;
            log.CreateTime = DateTime.Now;
            log.HTZ_ServerId = server.id;
            log.HTZ_ServerLog_Id = await db.GetNextIdentity_IntAsync();

            if (server.totalmemory > 0)
            {
                log.MemoryOccupancy = (int)((1 - ((double)server.freememory / server.totalmemory)) * 100);
            }

            log.ServerState = serverstate;
            log.State = v_common.YesState;

            await db.InsertAsync(log);
        }


    }
}
