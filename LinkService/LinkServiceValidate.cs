using Drision.Framework.Common;
using Drision.Framework.Entity.Common;
using Drision.Framework.Entity.HighTechZone;
using Drision.Framework.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using CommonBll;
using CommonBll.Models;
using static CommonBll.CommonHelper;

namespace LinkService
{
    public class LinkServiceValidate
    {
        public async static void Start()
        {
            while (true)
            {
                var waittime = 24 * 60 * 60 * 1000;

                try
                {
                    using (var db = new BizDataContext())
                    {
                        var temp = db.Set<T_Configuration>().Where(p => p.Configuration_Key == CommonHelper.LINKKEY).FirstOrDefault();
                        if (temp != null)
                        {
                            waittime = Int32.Parse(temp.Configuration_Value);
                        }

                        var services = await db.Set<T_HTZ_ServiceApp>().Where(p => p.State == v_common.YesState).ToListAsync();

                        foreach (var item in services)
                        {
                            if (item.App_IsEnable ?? false)
                            {
                                LinkValidate(item.HTZ_ServiceApp_Id, item.App_URL, (int)ServiceType.App, item.HTZ_ServiceApp_Name);
                            }

                            if (item.Web_IsEnable ?? false)
                            {
                                LinkValidate(item.HTZ_ServiceApp_Id, item.Web_URL, (int)ServiceType.Web, item.HTZ_ServiceApp_Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var e = new CrawlerException()
                    {
                        crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.Service,
                        exceptionbrief = "应用服务链路异常",
                        exceptionmessage = ex.Message,
                        statuscode = 501,
                        serviceid = 1
                    };
                    await CommonHelper.SaveException(e);
                }
                finally
                {
                    Thread.Sleep(waittime);
                }
            }

        }

        private static async void LinkValidate(int id, string url, int serviceType, string serviceName)
        {
            try
            {
                var app = new LinkService();

                app.OnCompleted += (async (s, e) =>
                {
                    if (e.StatusCode == HttpStatusCode.OK)
                    {
                        await CommonHelper.SaveNewState((int)HTZ_ServiceState_ServiceStateEnum.Fine, id, (int)e.Milliseconds, serviceType);
                    }
                    else
                    {
                        var ex = new CrawlerException()
                        {
                            crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.Service,
                            exceptionbrief = EnumHelper.GetDescription(e.StatusCode),
                            exceptionmessage = EnumHelper.GetDescription(e.StatusCode),
                            statuscode = (int)e.StatusCode,
                            serviceid = id,
                            servicename = serviceName,
                            serverAppType = serviceType
                        };
                        throw ex;
                    }
                });

                app.OnError += ((s, e) =>
                {
                    var ex = new CrawlerException()
                    {
                        crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.Service,
                        exceptionbrief = "请求时出错",
                        exceptionmessage = e.Exception.Message,
                        statuscode = (int)HttpStatusCode.InternalServerError,
                        serviceid = id,
                        servicename = serviceName,
                        serverAppType = serviceType,
                    };
                    throw ex;
                });

                await app.BeginRequest(new Uri(url));
            }
            catch (CrawlerException ex)
            {
                await CommonHelper.SaveException(ex);
            }
            catch (Exception ex)
            {
                var e = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.Service,
                    exceptionbrief = "应用服务链路异常",
                    exceptionmessage = ex.Message,
                    statuscode = 501,
                    serviceid = id,
                    servicename = serviceName
                };
                await CommonHelper.SaveException(e);
            }
        }
    }
}
