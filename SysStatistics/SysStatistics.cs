using SysStatistics.Models;
using Drision.Framework.Common;
using Drision.Framework.Entity.Gateway;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drision.Framework.Entity.Common;
using CommonBll.Models;
using Drision.Framework.Linq;

namespace GatewaySysStatisticsService
{

    public class SysStatistics
    {
        private static readonly string rootdir = System.IO.Directory.GetCurrentDirectory();

        private bool isStatistics
        {
            get
            {
                var temp = System.Configuration.ConfigurationManager.AppSettings["IsStatisticsTotalActiveUser"];
                if (temp == null)
                {
                    return false;
                }
                return temp.ToString().ToBool();
            }
        }

        public void Start()
        {


            if (isStatistics)
            {
                ActiveUsersByMonth();
            }

            //统计当月用户活跃度
            ActiveUsersByMonth_Update();
            //统计系统使用情况
            DoStatisticsMobile();
        }


        public async void ActiveUsersByMonth()
        {
            try
            {
                using (var db = new BizDataContext())
                {
                    var query =await db.Set<T_GW_LoginLog>().Where(p => p.State == v_common.YesState).Select(p => new
                    {
                        createtime = p.CreateTime,
                        cretetimestr = p.CreateTime.Value.ToString(),
                        year_int = p.CreateTime.Value.Year,
                        month_int = p.CreateTime.Value.Month,
                        ownerid = p.OwnerId,
                        monthval = p.CreateTime.Value.Month.ToString()
                    }).ToListAsync();


                    #region 用户表用户统计
                    string sql2 = @"select a.area_id, a.area_name, u.createtime ,u.loginname 
from T_user u
left join t_area a on u.opaccountcode = a.area_id
where u.state <> 9999";

                    DataTable dt2 = db.ExecuteDataTable(sql2);

                    var queryuser = dt2.Rows.OfType<DataRow>()
                        .Select(p => new
                        {
                            areaid = p["area_id"].ToString(),
                            areaname = p["area_name"].ToString(),
                            createtime = p["createtime"].ToString(),
                            loginname = p["loginname"].ToString(),
                        });

                    var modeluser = queryuser.ToList();
                    //用户表
                    var queryuser2 = modeluser.GroupBy(x => new { x.areaid, x.areaname }).
                        Select(x => new v_areauser { areaid = x.Key.areaid, areaname = x.Key.areaname, mulist = x.Select(p => new v_mu { createtime = Convert.ToDateTime(p.createtime) }).ToList() });

                    var modeluser2 = queryuser2.ToList();
                    #endregion

                    var model = query.ToList();
                    var query2 = model.GroupBy(x => new { x.area_id, x.area_name }).Select(x => new v_activeusers
                    {
                        areaid = x.Key.area_id,
                        areaname = x.Key.area_name,
                        users = x.GroupBy(p => new { p.month_int, p.createtimestr }).Select(p => new v_monthusers { month = p.Key.month_int, createtimestr = p.Key.createtimestr, countusers = p.Count() }).ToList()
                    }
                    );


                    var model2 = query2.ToList();
                    List<v_activeusers> aulist = new List<v_activeusers>();
                    List<v_monthusers> mulist = null; ;
                    v_activeusers au = null;
                    v_monthusers mu = null;

                    foreach (var x in model2)
                    {
                        au = new v_activeusers();
                        au.areaid = x.areaid;
                        au.areaname = x.areaname;
                        mulist = new List<v_monthusers>();

                        for (int i = 2; i <= 4; i++)
                        {
                            mu = new v_monthusers();
                            mu.month = i;
                            mu.createtimestr = new DateTime(2017, i, 1).ToString("yyyy-MM");
                            int mcount = 0;
                            int mtotalcount = x.users.Count();
                            foreach (var item in x.users)
                            {
                                mcount++;
                                if (item.month == i)
                                {
                                    mu.countusers = item.countusers;
                                    break;
                                }
                                else if (mcount == mtotalcount)
                                {
                                    mu.countusers = 0;
                                }
                            }
                            mulist.Add(mu);
                        }
                        au.users = mulist;
                        aulist.Add(au);
                    }
                    ///获取用户总数


                    List<T_GW_UserStatistics> entitylist = new List<T_GW_UserStatistics>();
                    T_GW_UserStatistics entity = null;
                    foreach (var item in aulist)
                    {
                        if (item.areaname == "")
                        {
                            var M = modeluser2.Where(x => x.areaname == item.areaname).FirstOrDefault().mulist;
                            foreach (var x in item.users)
                            {
                                entity = new T_GW_UserStatistics();
                                entity.GW_UserStatistics_Id = db.GetNextIdentity_Int();
                                entity.AreaId = 0;
                                entity.AreaName = "未填写";
                                entity.YearMonth = x.createtimestr;
                                entity.MonthVal = x.month;
                                entity.ActiveUserCount = x.countusers;
                                DateTime nextmonth = Convert.ToDateTime(x.createtimestr).AddMonths(1);
                                entity.UserCount = M.Where(p => p.createtime < nextmonth).Count();
                                entity.CreateTime = DateTime.Now;
                                entitylist.Add(entity);
                            }

                        }
                        else
                        {
                            var M = modeluser2.Where(x => x.areaname == item.areaname).FirstOrDefault().mulist;
                            foreach (var x in item.users)
                            {
                                entity = new T_GW_UserStatistics();
                                entity.GW_UserStatistics_Id = db.GetNextIdentity_Int();
                                entity.AreaId = item.areaid.ToInt();
                                entity.AreaName = item.areaname;
                                entity.YearMonth = x.createtimestr;
                                entity.MonthVal = x.month;
                                entity.ActiveUserCount = x.countusers;
                                DateTime nextmonth = Convert.ToDateTime(x.createtimestr).AddMonths(1);
                                entity.UserCount = M.Where(p => p.createtime < nextmonth).Count();
                                entity.CreateTime = DateTime.Now;
                                entitylist.Add(entity);
                            }
                        }

                    }
                    db.BatchInsert(entitylist);
                }

            }
            catch (Exception ex)
            {
                ErrorLog(ex, "FirstStatistics", rootdir);
            }
        }


        public void ActiveUsersByMonth_Update()
        {
            try
            {
                using (var db = new BizDataContext())
                {
                    string currenttime = DateTime.Now.ToString("yyyy-MM");

                    string sql = string.Format(@"select t6.year_int,t6.month_int,t6.createtimestr,t6.opaccountcode,t6.ownerid,t6.createtime,t7.area_name from 
  (
    select * from 
      (
        select to_number(to_char(t4.createtime, 'yyyy')) year_int,to_number(to_char(t4.createtime, 'mm')) month_int,to_char(t4.createtime, 'yyyy-mm')createtimestr,  t4.* from
          (
            select t3.opaccountcode,t2.ownerid, t2.createtime from t_user t3 
             inner join 
             (select t1.* from 
            (select row_number() over(partition by t0.ownerid, to_char(t0.createtime, 'yyyy-mm') order by  t0.createtime desc) rownumb, t0.ownerid, t0.phone, t0.createtime from t_Gw_Loginlog t0 
            order by t0.phone, t0.createtime desc) t1 where t1.rownumb = 1
            ) t2 on (t3.state <> 9999 and t2.ownerid = t3.user_id)
          ) t4
       ) t5 where t5.createtimestr = '{0}' 
   ) t6 
   left join T_area t7 on t6.opaccountcode = t7.area_id 
   order by t6.month_int", currenttime);




                    DataTable dt = db.ExecuteDataTable(sql);
                    var query = dt.Rows.OfType<DataRow>()
                        .Select(p => new
                        {
                            createtime = Convert.ToDateTime(p["createtime"]),
                            createtimestr = p["createtimestr"].ToString(),
                            year_int = Convert.ToInt32(p["year_int"]),
                            month_int = Convert.ToInt32(p["month_int"]),
                            ownerid = p["ownerid"].ToString(),
                            area_id = p["opaccountcode"].ToString(),
                            area_name = p["area_name"].ToString(),
                            monthval = p["month_int"].ToString(),
                        });


                    #region 用户表用户统计
                    string sql2 = @"select a.area_id, a.area_name, u.createtime ,u.loginname 
from T_user u
left join t_area a on u.opaccountcode = a.area_id
where u.state <> 9999";

                    DataTable dt2 = db.ExecuteDataTable(sql2);

                    var queryuser = dt2.Rows.OfType<DataRow>()
                        .Select(p => new
                        {
                            areaid = p["area_id"].ToString(),
                            areaname = p["area_name"].ToString(),
                            createtime = p["createtime"].ToString(),
                            loginname = p["loginname"].ToString(),
                        });

                    var modeluser = queryuser.ToList();
                    var queryuser2 = modeluser.GroupBy(x => new { x.areaid, x.areaname }).
                        Select(x => new v_areauser { areaid = x.Key.areaid, areaname = x.Key.areaname, mulist = x.Select(p => new v_mu { createtime = Convert.ToDateTime(p.createtime) }).ToList() });

                    var modeluser2 = queryuser2.ToList();
                    #endregion

                    var model = query.ToList();
                    var query2 = model.GroupBy(x => new { x.area_id, x.area_name }).Select(x => new v_activeusers
                    {
                        areaid = x.Key.area_id,
                        areaname = x.Key.area_name,
                        users = x.GroupBy(p => new { p.month_int, p.createtimestr }).Select(p => new v_monthusers { month = p.Key.month_int, createtimestr = p.Key.createtimestr, countusers = p.Count() }).ToList()
                    }
                    );


                    var model2 = query2.ToList();
                    List<v_activeusers> aulist = new List<v_activeusers>();
                    List<v_monthusers> mulist = null; ;
                    v_activeusers au = null;
                    v_monthusers mu = null;

                    foreach (var x in model2)
                    {
                        au = new v_activeusers();
                        au.areaid = x.areaid;
                        au.areaname = x.areaname;
                        mulist = new List<v_monthusers>();
                        foreach (var item in x.users)
                        {
                            mu = new v_monthusers();
                            mu.month = item.month;
                            mu.createtimestr = item.createtimestr;
                            mu.countusers = item.countusers;
                            mulist.Add(mu);
                        }
                        au.users = mulist;
                        aulist.Add(au);
                    }
                    ///获取用户总数


                    List<T_GW_UserStatistics> addlist = new List<T_GW_UserStatistics>();
                    List<T_GW_UserStatistics> updatelist = new List<T_GW_UserStatistics>();
                    T_GW_UserStatistics userstatistics = null;
                    foreach (var item in aulist)
                    {
                        if (item.areaname == "")
                        {
                            var M = modeluser2.Where(x => x.areaname == item.areaname).FirstOrDefault().mulist;
                            foreach (var x in item.users)
                            {
                                userstatistics = new T_GW_UserStatistics();

                                userstatistics.AreaId = 0;
                                userstatistics.AreaName = "未填写";
                                userstatistics.YearMonth = x.createtimestr;
                                userstatistics.MonthVal = x.month;
                                userstatistics.ActiveUserCount = x.countusers;
                                userstatistics.UserCount = M.Count();
                                var entity = db.Set<T_GW_UserStatistics>().Where(p => p.AreaId == 0 && p.AreaName == "未填写" && p.YearMonth == x.createtimestr).FirstOrDefault();
                                if (entity != null)
                                {
                                    entity.ActiveUserCount = userstatistics.ActiveUserCount;
                                    entity.UserCount = userstatistics.UserCount;
                                    entity.UpdateTime = DateTime.Now;
                                    db.UpdatePartial(entity, xx => new { xx.ActiveUserCount, xx.UserCount, xx.UpdateTime });
                                }
                                else
                                {
                                    userstatistics.GW_UserStatistics_Id = db.GetNextIdentity_Int();
                                    userstatistics.CreateTime = DateTime.Now;
                                    addlist.Add(userstatistics);
                                }
                            }

                        }
                        else
                        {
                            var M = modeluser2.Where(x => x.areaname == item.areaname).FirstOrDefault().mulist;
                            foreach (var x in item.users)
                            {
                                userstatistics = new T_GW_UserStatistics();
                                userstatistics.AreaId = item.areaid.ToInt();
                                userstatistics.AreaName = item.areaname;
                                userstatistics.YearMonth = x.createtimestr;
                                userstatistics.MonthVal = x.month;
                                userstatistics.ActiveUserCount = x.countusers;
                                userstatistics.UserCount = M.Count();

                                var entity = db.Set<T_GW_UserStatistics>().Where(p => p.AreaId == item.areaid.ToInt() && p.AreaName == item.areaname && p.YearMonth == x.createtimestr).FirstOrDefault();
                                if (entity != null)
                                {
                                    entity.ActiveUserCount = userstatistics.ActiveUserCount;
                                    entity.UserCount = userstatistics.UserCount;
                                    entity.UpdateTime = DateTime.Now;
                                    db.UpdatePartial(entity, xx => new { xx.ActiveUserCount, xx.UserCount, xx.UpdateTime });
                                }
                                else
                                {
                                    userstatistics.GW_UserStatistics_Id = db.GetNextIdentity_Int();
                                    userstatistics.CreateTime = DateTime.Now;
                                    addlist.Add(userstatistics);
                                }
                            }
                        }

                    }

                    db.BatchInsert(addlist);
                }
            }
            catch (Exception ex)
            {
                ErrorLog(ex, "每天统计用户活跃度", rootdir);
            }
        }


        public void DoStatisticsMobile()
        {
            try
            {
                using (var db = new BizDataContext())
                {

                    string sql = @"select t2.equipmenttype,t2.osversion from 
    (
      select row_number() over(partition by t0.ownerid order by  t0.createtime desc) rownumb, t0.equipmenttype, t0.osversion from t_Gw_Loginlog t0 
      inner join T_user t1 on (t1.state <> 9999 and t0.ownerid = t1.user_id)
        
      order by t0.ownerid, t0.createtime desc 
    ) t2 where t2.rownumb = 1";

                    DataTable dt = db.ExecuteDataTable(sql);

                    var query = dt.Rows.OfType<DataRow>()
                        .Select(p => new
                        {
                            equipmenttype = p["equipmenttype"].ToString(),
                            osversion = p["osversion"].ToString(),

                        });

                    var model = query.GroupBy(x => x.equipmenttype).Select(x => new
                    {
                        ostype = x.Key,
                        count = x.Count(),
                        vals = x
                    });
                    //获取所有数据
                    var modelList = db.Set<T_GW_MobileStatistics>().ToList();


                    List<T_GW_MobileStatistics> addlist = new List<T_GW_MobileStatistics>();
                    List<T_GW_MobileStatistics> updatelist = new List<T_GW_MobileStatistics>();
                    foreach (var x in model)
                    {
                        T_GW_MobileStatistics entity = new T_GW_MobileStatistics();
                        string ostype = x.ostype;
                        if (string.IsNullOrEmpty(x.ostype))
                        {
                            ostype = null;
                        }

                        entity.OSType = ostype;
                        entity.TotalCount = x.count;
                        entity.StatisticsType = "os";


                        if (modelList.Where(p => p.StatisticsType == "os" && p.OSType == ostype).FirstOrDefault() != null)
                        {
                            updatelist.Add(entity);
                        }
                        else
                        {
                            entity.GW_MobileStatistics_Id = db.GetNextIdentity_Int();
                            entity.CreateTime = DateTime.Now;
                            entity.State = 1;
                            addlist.Add(entity);
                        }


                        //string ostype = x.ostype;
                        foreach (var item in x.vals.GroupBy(p => p.osversion).Select(p => new { osvertion = p.Key, count = p.Count() }))
                        {
                            T_GW_MobileStatistics entity2 = new T_GW_MobileStatistics();
                            string osversion = item.osvertion;
                            if (string.IsNullOrEmpty(item.osvertion))
                            {
                                osversion = null;
                            }
                            entity2.OSType = ostype;
                            entity2.OSVersion = osversion;
                            entity2.TotalCount = item.count;
                            entity2.StatisticsType = "ver";


                            if (modelList.Where(p => p.StatisticsType == "ver" && p.OSType == ostype && p.OSVersion == osversion).FirstOrDefault() != null)
                            {
                                updatelist.Add(entity2);
                            }
                            else
                            {
                                entity2.GW_MobileStatistics_Id = db.GetNextIdentity_Int();
                                entity2.CreateTime = DateTime.Now;
                                entity2.State = 1;
                                addlist.Add(entity2);
                            }
                        }
                    }


                    foreach (var x in updatelist)
                    {
                        if (x.StatisticsType == "os")
                        {
                            var entity = db.Set<T_GW_MobileStatistics>().Where(p => p.StatisticsType == x.StatisticsType && p.OSType == x.OSType).FirstOrDefault();
                            if (entity != null)
                            {
                                entity.TotalCount = x.TotalCount;
                                entity.UpdateTime = DateTime.Now;
                                db.UpdatePartial(entity, xx => new { xx.TotalCount, xx.UpdateTime });
                            }
                        }
                        else if (x.StatisticsType == "ver")
                        {
                            var entity = db.Set<T_GW_MobileStatistics>().Where(p => p.StatisticsType == x.StatisticsType && p.OSType == x.OSType && p.OSVersion == x.OSVersion).FirstOrDefault();
                            if (entity != null)
                            {
                                entity.TotalCount = x.TotalCount;
                                entity.UpdateTime = DateTime.Now;
                                db.UpdatePartial(entity, xx => new { xx.TotalCount, xx.UpdateTime });
                            }
                        }
                    }

                    db.BatchInsert(addlist);
                }
            }
            catch (Exception ex)
            {
                ErrorLog(ex, "手机app版本号统计", rootdir);
            }
        }



        public void ErrorLog(Exception ex, string errortype, string rootdir)
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
