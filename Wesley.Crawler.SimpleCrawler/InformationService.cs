using CommonBll;
using CommonBll.Models;
using Drision.Framework.Common;
using Drision.Framework.Entity.Common;
using Drision.Framework.Entity.HighTechZone;
using Drision.Framework.Entity.Infomation;
using Drision.Framework.Linq;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static CommonBll.CommonHelper;

namespace SimpleCrawler
{
    /// <summary>
    /// 新闻消息抓取服务
    /// </summary>
    public class InformationService
    {
        /// <summary>
        /// 启动方法
        /// </summary>
        public async static void Start()
        {
            while (true)
            {
                var waitTime = milliscond_OneDay;

                var infoCrawlers = new List<v_crawler>();

                using (var db = new BizDataContext())
                {
                    var dbWaitTime = db.Set<T_Configuration>().Where(p => p.Configuration_Key == INFORMATIONKEY).FirstOrDefault();
                    if (dbWaitTime != null)
                    {
                        waitTime = Int32.Parse(dbWaitTime.Configuration_Value);
                    }

                    infoCrawlers = await db.Set<T_HTZ_CrawlerService>().Where(p => p.State == v_common.YesState && p.ServiceType == (int)HTZ_CrawlerService_ServiceTypeEnum.Information && p.IsEnable.Value).Select(p => new v_crawler
                    {
                        id = p.HTZ_CrawlerService_Id,
                        infotype = p.InfoType ?? 0,
                        name = p.HTZ_CrawlerService_Name,
                        xmlfile = p.XMLFilePath,
                        crawlertype = (int)HTZ_CrawlerService_ServiceTypeEnum.Information
                    }).ToListAsync();
                }

                foreach (var item in infoCrawlers)
                {
                    InfomationCrawler(item);
                }

                Thread.Sleep(waitTime);
            }
        }

        /// <summary>
        /// 新闻抓取方法
        /// </summary>
        /// <param name="infotype">抓取的新闻栏目类型</param>
        /// <param name="infoDoc"></param>
        public async static void InfomationCrawler(v_crawler crawler)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(crawler.xmlfile);

                    var rootNode = xmlDocument.SelectSingleNode("data");
                    if (rootNode == null)
                    {
                        throw new CrawlerException(crawler.id,"配置文件错误", "未找到主配置项data");
                    }

                    crawler.encode = DefaultEncode;

                    if (rootNode.Attributes["encode"] != null)
                    {
                        crawler.encode = rootNode.Attributes["encode"].Value;
                    }

                    crawler.url = rootNode.Attributes["url"].Value;


                    var infoCrawler = new SimpleCrawler();//新建一个抓取服务

                    infoCrawler.OnError += (s, e) =>
                    {
                        throw new CrawlerException(crawler.id, "获取页面代码时错误", e.Exception.Message);
                    };
                    infoCrawler.OnCompleted += async (s, e) =>
                    {

                        try
                        {
                            using (var db = new BizDataContext())
                            {
                                SaveInfomation(e.PageSource, crawler, xmlDocument, db);

                                await CommonHelper.SaveNewState((int)HTZ_ServiceState_ServiceStateEnum.Fine, crawler.id);
                            }
                        }
                        catch (Exception ex)
                        {
                            var messageException= new CrawlerException(crawler.id, "解析出错", ex.Message);
                            await SaveException(messageException);
                        }
                    };

                    if (!string.IsNullOrEmpty(crawler.url))
                    {
                        infoCrawler.Start(new Uri(crawler.url), crawler.encode).Wait();
                    }
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
                        exceptionbrief = "信息抓取服务错误",
                        exceptionmessage = ex.Message,
                        statuscode = 501,
                        serviceid = 2
                    };
                    await CommonHelper.SaveException(e);
                }
            });
        }

        /// <summary>
        /// 抓取青网本地信息
        /// </summary>
        public static void InfomationDetailCrawler(string url, XmlNode infoNode, T_Information info, v_crawler crawler, string encode)
        {
            var infoDetailCrawler = new SimpleCrawler();//新建一个爬虫服务
            infoDetailCrawler.OnError += (s, e) =>
            {
                var ee = new CrawlerException()
                {
                    crawlertype = (int)HTZ_ExceptionHandler_ServiceTypeEnum.DataGrab,
                    exceptionbrief = "详情抓取出错",
                    exceptionmessage = e.Exception.Message,
                    statuscode = 500,
                    serviceid = crawler.id
                };
                throw ee;
            };
            infoDetailCrawler.OnCompleted += async (s, e) =>
            {

                try
                {
                    using (var db = new BizDataContext())
                    {
                        await SaveInfomationDetail(e.PageSource, info, infoNode, db, crawler, url);
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
                    await CommonHelper.SaveException(ee);
                }
            };

            infoDetailCrawler.Start(new Uri(url), encode).Wait();//没被封锁就别使用代理：60.221.50.118:8090
        }

        private async static Task SaveInfomationDetail(string pageSource, T_Information info, XmlNode infoNode, BizDataContext db, v_crawler crawler, string detailUrl)
        {
            var rootNode = CommonHelper.GetRootNode(pageSource);

            if (info != null)
            {
                var xmlRoot = infoNode.SelectSingleNode("DataDetail");

                var detailHost = new Regex(@"\S+/").Match(detailUrl).Value;

                //新闻解析应该包含多个子节点，每个子节点表示一个属性，这里进行循环赋值
                foreach (XmlNode property in xmlRoot.ChildNodes)
                {
                    info = CommonHelper.GetProperty(info, rootNode, property, detailHost);
                }


                var info_infotag = new T_InfoType_Information()
                {
                    InfoType_Information_Id = await db.GetNextIdentity_IntAsync(),
                    CreateTime = DateTime.Now,
                    InformationId = info.Information_Id,
                    InfoTypeId = crawler.infotype,
                };

                var informationcontent = new T_Information_Content()
                {
                    Information_Content_Id = await db.GetNextIdentity_IntAsync(),
                    Conent = info.Content,
                    ContentType = (int)Information_Content_ContentTypeEnum.TextWords,

                    OrderIndex = 0,
                    InformationId = info.Information_Id,
                    State = 0,
                    CreateTime = DateTime.Now,
                };

                info.ClassifyId = (int)InfoSource.Crawler;
                info.PublishTime = info.CreateTime;
                info.IsTop = false;


              

                if (!string.IsNullOrEmpty(informationcontent.Conent))
                {
                    var regex = new Regex("<img.*?>");
                    var imgMtach = regex.Match(info.Content);
                    if (imgMtach.Success)
                    {
                        var img = imgMtach.Value;
                        var srcMatch = new Regex("src=\".*?\"").Match(img);
                        if (srcMatch.Success)
                        {
                            var src = srcMatch.Value;

                            var att = new T_Attachment()
                            {
                                Attachment_ID = await db.GetNextIdentity_IntAsync(),
                                FilePath = src,

                                State = 0,
                                CreateTime = DateTime.Now,
                            };
                            await db.InsertAsync(att);
                        }
                    }

                    await db.InsertAsync(info);
                    await db.InsertAsync(info_infotag);
                    await db.InsertAsync(informationcontent);
                }


            }

        }

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="html">原始html</param>
        /// <param name="crawler">抓取服务配置</param>
        /// <param name="infoNode">xml配置数据</param>
        /// <param name="db">数据库链接</param>
        /// <param name="encode">html编码格式</param>
        /// <param name="url">抓取原始url</param>
        /// <returns></returns>
        public static void SaveInfomation(string html, v_crawler crawler, XmlDocument infoNode, BizDataContext db)
        {
            //页面数据的根节点
            var rootNode = CommonHelper.GetRootNode(html);

            //配置数据的根节点
            var xmlRoot = infoNode.SelectSingleNode("data");

            //获取信息列表的配置节点
            var listConfig = xmlRoot.SelectSingleNode("ListConfig");

            //这里是为了最终找到modelListNodes变量的值，也就是新闻列表
            //在此之前可能需要多次的剥离无效数据，因此使用了foreach循环
            //正常流程应该是多次SelectSingleNode后，进行一次SelectNodes，获取到新闻列表
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
                    //最终获取到信息列表，此时应该循环结束
                    modelListNodes = modelNode.SelectNodes(item.Attributes["signstr"].Value);
                    break;
                }
            }

            //获取对新闻实体解析的配置节点
            var infoConfig = xmlRoot.SelectSingleNode("InfoConfig");

            //对上面获取到的新闻列表循环处理
            foreach (HtmlNode info in modelListNodes)
            {
                T_Information entity = new T_Information();

                var detailUrl = string.Empty;

                //新闻解析应该包含多个子节点，每个子节点表示一个属性，这里进行循环赋值
                foreach (XmlNode property in infoConfig.ChildNodes)
                {
                    if (property.Name == "property")
                    {
                        entity = CommonHelper.GetProperty(entity, info, property);
                    }
                    else if (property.Name == "DetailUrl")
                    {
                        detailUrl = GetUrl(info, property, crawler.url);
                    }
                }

                var count = db.Set<T_Information>().Where(p => p.Information_Id == entity.Information_Id).Select(p => 1).Count();

                if (count >= 1)
                {
                    return;
                }

                entity.State = (int)T_InformationStateEnum.Publish;
                entity.InformationType = (int)t_informationtypeenum.customcontent;
                entity.InfoTypeIds = crawler.infotype + ",";


                if (!string.IsNullOrEmpty(detailUrl))
                {
                    //循环赋值完成后，前往新闻详情页获取新闻详情，完善新闻实体
                    InfomationDetailCrawler(detailUrl, xmlRoot, entity, crawler, crawler.encode);
                }

                entity.OriginalSourceUrl = detailUrl;


            }
        }

        /// <summary>
        /// 获取新闻详情的url
        /// </summary>
        /// <param name="info"></param>
        /// <param name="property"></param>
        /// <param name="homeUrl"></param>
        /// <returns></returns>
        private static string GetUrl(HtmlNode info, XmlNode property, string homeUrl)
        {


            HtmlNode currentNode = null;
            string result = string.Empty;

            foreach (XmlNode op in property.ChildNodes)
            {
                if (op.Name == "OperationItem")
                {
                    if (currentNode == null)
                    {
                        currentNode = info.SelectSingleNode(op.Attributes["signstr"].Value);
                    }
                    else
                    {
                        currentNode = currentNode.SelectSingleNode(op.Attributes["signstr"].Value);
                    }

                    if (currentNode == null)
                    {
                        return string.Empty;
                    }
                }
                else if (op.Name == "ResultItem")
                {
                    var value = string.Empty;
                    if (op.Attributes["isinnertext"] != null)
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

                    result = value;
                }
            }

            if (!(result.StartsWith("http") || result.StartsWith("https")))
            {
                homeUrl = new Regex(@"\S+/").Match(homeUrl).Value;

                if (result.StartsWith("./"))
                {
                    result = result.Replace("./", homeUrl);
                    return result;
                }
                else if (result.StartsWith("/"))
                {
                    homeUrl = new Regex(@"(http://|https://)?([^/]*)").Match(homeUrl).Value;
                }

                result = homeUrl + result;
            }

            return result;
        }

        private static string ConvertEncode(string origin)
        {
            //声明字符集   
            System.Text.Encoding utf8, gb2312;
            //utf8   
            utf8 = System.Text.Encoding.GetEncoding("utf-8");
            //gb2312   
            gb2312 = System.Text.Encoding.GetEncoding("gb2312");
            byte[] utf;
            utf = utf8.GetBytes(origin);
            utf = System.Text.Encoding.Convert(utf8, gb2312, utf);
            //返回转换后的字符   
            return gb2312.GetString(utf);
        }
    }
}
