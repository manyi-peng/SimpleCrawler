using Drision.Framework.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Drision.Framework.Entity.HighTechZone;
using CommonBll.Models;
using Drision.Framework.Linq;

namespace CommonBll
{
    public class CommonHelper
    {
        public const string WEATHERKEY = "WEATHERWAITTIME";
        public const string INFORMATIONKEY = "INFORMATIONWAITTIME";
        public const string LINKKEY = "LINKWAITTIME";
        public const string SERVERKEY = "SERVERWAITTIME";
        public const string DefaultEncode = "utf-8";

        public const int milliscond_OneDay = 24 * 60 * 60 * 1000;
        public const int InnerError = 500;

        /// <summary>
        /// 将html文本转化为htmlnode
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static HtmlNode GetRootNode(string html)
        {
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(html);
            return document.DocumentNode;
        }

        /// <summary>
        /// 根据配置项，解析对应属性并通过反射赋值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">要赋值的实体</param>
        /// <param name="info">抓取的html元素</param>
        /// <param name="property">xml配置</param>
        /// <param name="url">抓取的链接url，用于替换内容的相对路径</param>
        /// <returns></returns>
        public static T GetProperty<T>(T entity, HtmlNode info, XmlNode property, string url = null)
        {

            HtmlNode currentNode = null;

            foreach (XmlNode op in property.ChildNodes)
            {
                if (op.Name == "OperationItem")
                {
                    if (currentNode == null)
                    {
                        currentNode = info;
                    }

                    currentNode = currentNode.SelectSingleNode(op.Attributes["signstr"].Value);

                    if (op.Attributes["maybenull"] != null)
                    {
                        if (currentNode == null)
                        {
                            if (op.Attributes["isend"] == null)
                            {
                                break;
                            }
                            else
                            {
                                return entity;
                            }
                        }

                    }

                }
                else if (op.Name == "ResultItem")
                {
                    var value = string.Empty;

                    if (op.Attributes["isinnerhtml"] != null)
                    {
                        value = currentNode.InnerHtml.Trim();
                    }
                    else if (op.Attributes["isinnertext"] != null)
                    {
                        value = currentNode.InnerText.Trim();
                    }
                    else
                    {
                        value = currentNode.Attributes[op.Attributes["attributename"].Value].Value;
                    }

                    if (op.HasChildNodes)
                    {
                        var temp = value;
                        foreach (XmlNode item in op.ChildNodes)
                        {
                            temp = new Regex(item.Attributes["regex"].Value).Match(temp).Value;
                        }
                        value = temp;
                    }

                    if (op.Attributes["needReplaceUrl"] != null)
                    {
                        value = value.Replace(op.Attributes["ReplaceTag"].Value, url);
                    }

                    ReflectionHelper.SetObjectPropertyValue<T>(entity, op.Attributes["name"].Value, op.Attributes["type"].Value, value);
                }
            }

            return entity;
        }

        /// <summary>
        /// 保存异常信息
        /// </summary>
        /// <returns></returns>
        public async static Task SaveException(CrawlerException e)
        {
            using (var db = new BizDataContext())
            {
                var his = new T_HTZ_ExceptionHistory();

                his.CreateTime = DateTime.Now;
                his.ExceptionBreif = e.exceptionbrief;
                his.ExceptionMessage = e.exceptionmessage;
                his.HTZ_ExceptionHistory_Id = await db.GetNextIdentity_IntAsync();
                his.IsRead = false;
                his.ServiceId = e.serviceid;
                his.State = v_common.YesState;
                his.StatusCode = e.statuscode;
                his.ServiceType = e.serverAppType;//链接服务监控时，是App还是web

                await db.InsertAsync(his);

                await SendErrorMessage(db, e);
            }

            await SaveNewState((int)HTZ_ServiceState_ServiceStateEnum.Wrong, e.serviceid, null, e.serverAppType);
        }

        /// <summary>
        /// 保存最新服务状态
        /// </summary>
        /// <param name="servicestate">服务状态</param>
        /// <param name="serviceid">服务id</param>
        /// <param name="costTime">花费时间</param>
        /// <param name="serverAppType">链接服务监控时，是App还是web</param>
        /// <returns></returns>
        public async static Task SaveNewState(int servicestate, int serviceid, int? costTime = null, int? serverAppType = null)
        {
            using (var db = new BizDataContext())
            {
                var entity = await db.Set<T_HTZ_ServiceState>().Where(p => p.State == v_common.YesState && p.ObjectId == serviceid && p.ServiceType == serverAppType).FirstOrDefaultAsync();

                if (entity == null)
                {
                    entity = new T_HTZ_ServiceState();
                    entity.CreateTime = DateTime.Now;
                    entity.HTZ_ServiceState_Id = await db.GetNextIdentity_IntAsync();
                    entity.ServiceState = servicestate;
                    entity.ServiceType = serverAppType;//链接服务监控时，是App还是web
                    entity.State = v_common.YesState;
                    entity.ObjectId = serviceid;
                    entity.RequestCostTime = costTime;
                    entity.UpdateTime = entity.CreateTime;

                    await db.InsertAsync(entity);
                }
                else
                {
                    entity.UpdateTime = DateTime.Now;
                    entity.ServiceState = servicestate;
                    entity.RequestCostTime = costTime;

                    await db.UpdatePartialAsync(entity, p => new { p.UpdateTime, p.ServiceState, p.RequestCostTime });
                }
            }
        }

        public async static Task SendErrorMessage(BizDataContext db, CrawlerException e)
        {
            string msg = string.Format("{0},发生了异常，异常信息为：<br/>{1}<br/>更多信息请打开高新区智慧生活后台查看。", e.servicename, e.exceptionbrief);

            var handlers = await db.Set<T_HTZ_ExceptionHandler>().Where(p => p.ServiceType == e.crawlertype && p.State == v_common.YesState).ToListAsync();

            var nearLog = await db.Set<T_HTZ_ExceptionHandlerLog>().Where(p => p.ObjectId == e.serviceid && p.State == v_common.YesState).OrderByDescending(p => p.HTZ_ExceptionHandlerLog_Id).FirstOrDefaultAsync();

            if (nearLog != null && nearLog.CreateTime > DateTime.Now.AddHours(-8))
            {
                return;
            }

            var mails = handlers.Where(p => !string.IsNullOrEmpty(p.Email)).Select(p => p.Email).ToArray();

            if (mails.Length < 1)
            {
                return;
            }

            var mail = new MyMailManager();
            mail.SetMailMessageAndSend(msg, mails);

            var log = new T_HTZ_ExceptionHandlerLog();
            log.CreateTime = DateTime.Now;
            log.HandlerIdStr = string.Join(",", handlers.Select(p => p.HTZ_ExceptionHandler_Id).ToArray());
            log.HTZ_ExceptionHandlerLog_Id = await db.GetNextIdentity_IntAsync();
            log.Message = msg;
            log.MessageType = (int)HTZ_ExceptionHandlerLog_MessageTypeEnum.Mail;
            log.ObjectId = e.serviceid;
            log.State = v_common.YesState;
            await db.InsertAsync(log);
        }

        /// <summary>
        /// 信息来源，表示新闻是抓取的还是系统添加的
        /// </summary>
        public enum InfoSource
        {
            Crawler = 0,
            System = 1
        }

        /// <summary>
        /// 服务类型，测试的链接是app的还是web的
        /// </summary>
        public enum ServiceType
        {
            App = 0,
            Web = 1
        }
    }
}
