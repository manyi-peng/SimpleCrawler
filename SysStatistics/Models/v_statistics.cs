using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysStatistics.Models
{
    public class v_statisticsinfo
    {
        public int? id { get; set; } //PK
        public string mobile { get; set; }
        public string imei { get; set; }
        public DateTime? loginintime { get; set; }
        public DateTime? loginouttime { get; set; }
        public string equipmenttype { get; set; }
        public string osversion { get; set; }
        public int? ownerid { get; set; }
        public DateTime? createtime { get; set; }
        public DateTime? updatetime { get; set; }

        public string createtimestr { get; set; }

        public int? areaid { get; set; }
        public string areaname { get; set; }
        

    }


    public class v_loginlog
    {
        public int? id { get; set; } //PK
        public string mobile { get; set; }
        public string imei { get; set; }
        public DateTime? loginintime { get; set; }
        public DateTime? loginouttime { get; set; }
        public string equipmenttype { get; set; }
        public string osversion { get; set; }
        public int? ownerid { get; set; }
        public DateTime? createtime { get; set; }
        public DateTime? updatetime { get; set; }

        public string createtimestr { get; set; }

    }









    public class v_activeusers
    {
        public string areaid { get; set; }
        public string areaname { get; set; }
        public List<v_monthusers> users { get; set; }
    }

    public class v_monthusers
    {
        public int month { get; set; }
        public string createtimestr { get; set; }
        public int countusers { get; set; }
    }


    public class v_usermonthstatistics
    {
        public int id { get; set; }
        public int areaid { get; set; }
        public string areaname { get; set; }
        public string yearmonth { get; set; }
        public int activeusercount { get; set; }
        public int usercount { get; set; }
    }

    public class v_userstatistics
    {
        public int? id { get; set; }
        public int? areaid { get; set; }
        public string areaname { get; set; }
        public string yearmonth { get; set; }
        public int? monthval { get; set; }
        public int? activeusercount { get; set; }
        public int? usercount { get; set; }
    }



    /// 统计总用户
    /// 

    public class v_area_mu
    {
        public int? areaid { get; set; }
        public string areaname { get; set; }
        public DateTime? createtime { get; set; }
        public string loginname { get; set; }
    }

    public class v_areauser
    {
        public string areaid { get; set; }
        public string areaname { get; set; }
        public List<v_mu> mulist { get; set; }
    }
    public class v_mu
    {
        public DateTime? createtime {get;set;}
    }


    public class v_mobilestatistics
    {
        public int id { get; set; }  //PK
        public string ostype { get; set; }  // android, ios
        public string osversion { get; set; } //版本号
        public int? totaltcount { get; set; } //统计总数
        public string statisticstype { get; set; } //os 统计系统类型 ver 统计系统版本

    }
    public class v_mobilestatistics_model
    {
        public List<v_mobilestatistics> os_statistics { get; set; }
        public List<v_mobilestatistics> androidver_statistics { get; set; }
        public List<v_mobilestatistics> iosver_statistics { get; set; }  

    }

    public class v_osstatistics_m
    {
        public string ostype { get; set; }
        public int count { get; set; }
    }

    public class v_verstatistics_m
    {
        public string versiontype { get; set; }
        public int count { get; set; }
    }


    public class v_hlj_userstatistics
    {
        public int areaid { get; set; }
        public string areaname { get; set; }
        public DateTime createtime { get; set; }
        public string createtimestr { get; set; }
        public int totalusers { get; set; }
        public int storeid { get; set; }
        public string storename { get; set; }
        public int level { get; set; }
        public int logincount { get; set; }
    }

    public class v_hlj_recomuserstatistics
    {
        public int? userid { get; set; }
        public int number { get; set; }//序号
        public int? areaid { get; set; }
        public string areaname { get; set; }
        public DateTime createtime { get; set; }
        public string createtimestr { get; set; }
        public int totalusers { get; set; }
        public int? storeid { get; set; }
        public string storename { get; set; }
        public int level { get; set; }
        public int logincount { get; set; }
        public string user_officephone { get; set; }//推荐人手机号

        public int usercount { get; set; } //推荐注册用户数
        public string begintimestr { get; set; }
        public string endtimestr { get; set; }

        public List<v_hlj_recomuserstatistics> recomuserstatistics { get; set; }
        public List<v_user> userlist { get; set; }
    }


}
