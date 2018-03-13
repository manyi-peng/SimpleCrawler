using System.ComponentModel;

namespace CommonBll.Models
{
    public class v_information
    {

    }

    public static class v_common
    {
        public static readonly int YesState = 1;
    }

    /// <summary>
    /// 信息类型：链接/自定义内容
    /// </summary>
    public enum t_informationtypeenum
    {
        [Description("链接")]
        link = 0,
        [Description("自定义内容")]
        customcontent = 1,

    }
}
