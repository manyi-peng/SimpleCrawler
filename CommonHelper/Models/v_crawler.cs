namespace CommonBll.Models
{
    public class v_crawler
    {
        public int id { get; set; }

        public string name { get; set; }

        public int crawlertype { get; set; }

        /// <summary>
        /// 新闻抓取时，绑定的新闻栏目类型id
        /// </summary>
        public int infotype { get; set; }

        public string xmlfile { get; set; }

        /// <summary>
        /// 抓取页面编码格式
        /// </summary>
        public string encode { get; set; }

        /// <summary>
        /// 抓取页面链接
        /// </summary>
        public string url { get; set; }
    }
}
