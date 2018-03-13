using Drision.Framework.Entity.HighTechZone;
using System;

namespace CommonBll
{
    public class CrawlerException : Exception
    {
        /// <summary>
        /// 服务id
        /// </summary>
        public int serviceid { get; set; }

        /// <summary>
        /// 请求状态码
        /// </summary>
        public int statuscode { get; set; }

        /// <summary>
        /// 错误详细信息
        /// </summary>
        public string exceptionmessage { get; set; }

        /// <summary>
        /// 错误简要信息
        /// </summary>
        public string exceptionbrief { get; set; }

        /// <summary>
        /// 链接服务监控时，是App还是web
        /// </summary>
        public int? serverAppType
        {
            get; set;
        }

        /// <summary>
        /// 抓取服务类型
        /// </summary>
        public int crawlertype { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string servicename { get; set; }

        public CrawlerException() : base()
        {
            SetDefaultData();
        }

        public CrawlerException(int serviceid,string brief,string msg)
        {
            SetDefaultData();
            this.serviceid = serviceid;
            this.exceptionmessage = msg;
            this.exceptionbrief = brief;
        }

        private void SetDefaultData()
        {
            statuscode = CommonHelper.InnerError;
            crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab;
        }
    }
}
