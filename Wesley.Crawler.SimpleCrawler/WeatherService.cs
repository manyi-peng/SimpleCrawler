using Drision.Framework.Common;
using Drision.Framework.Entity.HighTechZone;
using Drision.Framework.Entity.WZDecisionSupport;
using Drision.Framework.Linq;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Drision.Framework.Entity.Common;
using CommonBll.Models;
using CommonBll;

namespace SimpleCrawler
{
    public class WeatherService
    {
        public async static void Start()
        {
            while (true)
            {
                var waittime = 24 * 60 * 60 * 1000;
                try
                {
                    var crawler = new v_crawler();

                    using (var db = new BizDataContext())
                    {
                        //从数据库获取抓取间隔
                        var temp = db.Set<T_Configuration>().Where(p => p.Configuration_Key == CommonHelper.WEATHERKEY).FirstOrDefault();
                        if (temp != null)
                        {
                            waittime = Int32.Parse(temp.Configuration_Value);
                        }

                        //从数据库获取抓取服务的配置
                        crawler = await db.Set<T_HTZ_CrawlerService>().Where(p => p.State == v_common.YesState && p.ServiceType == (int)HTZ_CrawlerService_ServiceTypeEnum.Weather && p.IsEnable.Value).Select(p => new v_crawler
                        {
                            id = p.HTZ_CrawlerService_Id,
                            infotype = p.InfoType ?? 0,
                            name = p.HTZ_CrawlerService_Name,
                            xmlfile = p.XMLFilePath,
                            crawlertype = (int)HTZ_CrawlerService_ServiceTypeEnum.Weather
                        }).FirstOrDefaultAsync();
                    }


                    //开始天气抓取
                    WeatherCrawler(crawler);

                    //开始生活指数抓取
                    LivingIndexCrawler(crawler);

                    //记录服务状态
                    await CommonHelper.SaveNewState((int)HTZ_ServiceState_ServiceStateEnum.Fine, crawler.id);
                }
                catch (CrawlerException ex)
                {
                    await CommonHelper.SaveException(ex);
                }
                catch (Exception ex)
                {
                    var e = new CrawlerException()
                    {
                        crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                        exceptionbrief = "天气抓取服务错误",
                        exceptionmessage = ex.Message,
                        statuscode = 501,
                        serviceid = 2
                    };
                    await CommonHelper.SaveException(e);
                }
                finally
                {
                    Thread.Sleep(waittime);
                }
            }
        }


        /// <summary>
        /// 抓取天气信息
        /// </summary>
        public static void WeatherCrawler(v_crawler crawler)
        {
            //获取xml配置文件
            var cfg = new XmlDocument();
            cfg.Load(crawler.xmlfile);

            var rootNode = cfg.SelectSingleNode("data");
            if (rootNode == null)
            {
                var e = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                    exceptionbrief = "配置文件出错",
                    exceptionmessage = "未找到主配置项data",
                    statuscode = 500,
                    serviceid = crawler.id
                };
                throw e;
            }

            var tideCrawler = new SimpleCrawler();//新建一个爬虫

            //抓取错误的处理
            tideCrawler.OnError += (s, e) =>
            {
                var ex = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                    exceptionbrief = "抓取出错",
                    exceptionmessage = e.Exception.Message,
                    statuscode = 500,
                    serviceid = crawler.id
                };
                throw ex;
            };

            //抓取成功后的解析
            tideCrawler.OnCompleted += async (s, e) =>
            {

                try
                {
                    using (var db = new BizDataContext())
                    {
                        await SaveWeekData(e.PageSource, rootNode, db);
                    }
                }
                catch (Exception ex)
                {
                    var ee = new CrawlerException()
                    {
                        crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                        exceptionbrief = "解析出错",
                        exceptionmessage = ex.Message,
                        statuscode = 500,
                        serviceid = crawler.id
                    };
                    throw ee;
                }
            };


            //获取抓取url
            var url = rootNode.Attributes["url"].Value;

            //获取编码格式
            var encode = "utf-8";

            if (rootNode.Attributes["encode"] != null)
            {
                encode = rootNode.Attributes["encode"].Value;
            }

            //启动抓取
            if (!string.IsNullOrEmpty(url))
            {
                tideCrawler.Start(new Uri(url), encode).Wait();
            }
        }

        /// <summary>
        /// 抓取生活指数信息
        /// </summary>
        public static void LivingIndexCrawler(v_crawler crawler)
        {
            var cfg = new XmlDocument();
            cfg.Load(crawler.xmlfile);

            var rootNode = cfg.SelectSingleNode("data");
            if (rootNode == null)
            {
                var e = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                    exceptionbrief = "配置文件出错",
                    exceptionmessage = "未找到主配置项data",
                    statuscode = 500,
                    serviceid = crawler.id
                };
                throw e;
            }

            var livingIndexCrawler = new SimpleCrawler();//调用刚才写的爬虫程序
            livingIndexCrawler.OnError += (s, e) =>
            {
                var ee = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                    exceptionbrief = "生活指数抓取出错",
                    exceptionmessage = e.Exception.Message,
                    statuscode = 500,
                    serviceid = crawler.id
                };
                throw ee;
            };
            livingIndexCrawler.OnCompleted += async (s, e) =>
            {

                try
                {
                    using (var db = new BizDataContext())
                    {
                        await SaveLivingIndexData(e.PageSource, rootNode, db);
                    }
                }
                catch (Exception ex)
                {
                    var ee = new CrawlerException()
                    {
                        crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                        exceptionbrief = "详情解析出错",
                        exceptionmessage = ex.Message,
                        statuscode = 500,
                        serviceid = crawler.id
                    };
                    throw ee;
                }
            };


            var url = rootNode.SelectSingleNode("LivingIndexConfig").Attributes["url"].Value;
            var encode = "utf-8";

            if (rootNode.Attributes["encode"] != null)
            {
                encode = rootNode.Attributes["encode"].Value;
            }

            if (!string.IsNullOrEmpty(url))
            {
                livingIndexCrawler.Start(new Uri(url), encode).Wait();
            }
        }

        public async static Task SaveWeekData(string html, XmlNode xmlRoot, BizDataContext db)
        {
            var rootNode = CommonHelper.GetRootNode(html);

            //获取信息列表的配置节点
            var listConfig = xmlRoot.SelectSingleNode("ListConfig");

            //这里是为了最终找到modelListNodes变量的值，也就是列表
            //在此之前可能需要多次的剥离无效数据，因此使用了foreach循环
            //正常流程应该是多次SelectSingleNode后，进行一次SelectNodes，获取到列表
            HtmlNode modelNode = null;
            HtmlNodeCollection modelListNodes = null;
            foreach (XmlNode item in listConfig.ChildNodes)
            {
                if (modelNode == null)
                {
                    modelNode = rootNode;
                }

                if (item.Attributes["issingleselect"].Value.ToBool())
                {
                    //多次剥离无效数据
                    modelNode = modelNode.SelectSingleNode(item.Attributes["signstr"].Value);
                }
                else
                {
                    //最终获取到列表，此时应该循环结束
                    modelListNodes = modelNode.SelectNodes(item.Attributes["signstr"].Value);
                    break;
                }
            }

            //获取对实体解析的配置节点
            var infoConfig = xmlRoot.SelectSingleNode("WeatherConfig");

            var date = DateTime.Now;

            var weatherList = await db.Set<T_TemperatureHumidity>().Where(p => p.ActionDate >= DateTime.Now.Date).ToListAsync();

            //对上面获取到的列表循环处理
            foreach (HtmlNode info in modelListNodes)
            {

                T_TemperatureHumidity entity = new T_TemperatureHumidity();

                var detailUrl = string.Empty;

                //解析应该包含多个子节点，每个子节点表示一个属性，这里进行循环赋值
                foreach (XmlNode property in infoConfig.ChildNodes)
                {
                    entity = CommonHelper.GetProperty(entity, info, property);
                }

                entity.State = v_common.YesState;
                entity.ActionDate = date.Date;
                entity.ActionYear = date.Year.ToString();
                entity.CreateTime = DateTime.Now;

                var id = weatherList.Where(p => p.ActionDate == date.Date).Select(p => p.TemperatureHumidity_Id).FirstOrDefault();

                if (id > 0)
                {
                    entity.TemperatureHumidity_Id = id;
                    entity.UpdateTime = DateTime.Now;

                    await db.UpdatePartialAsync(entity, p => new { p.TemperatureStr, p.Wheather, p.Wind, p.WindDirection, p.UpdateTime });
                }
                else
                {
                    entity.TemperatureHumidity_Id = await db.GetNextIdentity_IntAsync();
                    await db.InsertAsync(entity);
                }

                date = date.AddDays(1);
            }
        }

        public async static Task SaveLivingIndexData(string html, XmlNode xmlRoot, BizDataContext db)
        {
            var rootNode = CommonHelper.GetRootNode(html);

            //获取对实体解析的配置节点
            var infoConfig = xmlRoot.SelectSingleNode("LivingIndexConfig");

            var date = DateTime.Now;


            var entity = new T_HTZ_LivingIndex();


            //解析应该包含多个子节点，每个子节点表示一个属性，这里进行循环赋值
            foreach (XmlNode property in infoConfig.ChildNodes)
            {
                entity = CommonHelper.GetProperty(entity, rootNode, property);
            }

            //查询天气数据
            var dbentity = (from a in db.Set<T_HTZ_LivingIndex>()

                            where a.CreateTime == DateTime.Now.Date

                            select a).FirstOrDefault();

            if (dbentity != null)
            {
                entity.HTZ_LivingIndex_Id = dbentity.HTZ_LivingIndex_Id;
                entity.CreateTime = dbentity.CreateTime;
                entity.UpdateTime = DateTime.Now;

                await db.UpdateAsync(entity);
            }
            else
            {
                entity.HTZ_LivingIndex_Id = db.GetNextIdentity_Int();
                entity.CreateTime = DateTime.Now.Date;

                await db.InsertAsync(entity);
            }

        }
    }
}
