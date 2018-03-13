using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll
{
    public class Logs
    {
        /// <summary>
        /// 输入文本日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(string msg)
        {
            string strPath;                                                   //文件的路径
            DateTime dt = DateTime.Now;
            try
            {
                strPath = Directory.GetCurrentDirectory() + "\\Log";          //winform工程\bin\目录下 创建日志文件夹 

                if (Directory.Exists(strPath) == false)                          //工程目录下 Log目录 '目录是否存在,为true则没有此目录
                {
                    Directory.CreateDirectory(strPath);                       //建立目录　Directory为目录对象
                }
                strPath = strPath + "\\" + dt.ToString("yyyy");

                if (Directory.Exists(strPath) == false)
                {
                    Directory.CreateDirectory(strPath);
                }
                strPath = strPath + "\\" + dt.Year.ToString() + "-" + dt.Month.ToString() + ".txt";

                StreamWriter FileWriter = new StreamWriter(strPath, true);           //创建日志文件
                FileWriter.WriteLine("[" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "]  " + msg);
                FileWriter.Close();                                                 //关闭StreamWriter对象
            }
            catch (Exception ex)
            {
                string str = ex.Message.ToString();
            }
        }

        /// <summary>
        /// 输入错误日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(Exception msg)
        {
            string strPath;                                                   //文件的路径
            DateTime dt = DateTime.Now;
            try
            {
                strPath = Directory.GetCurrentDirectory() + "\\Log";          //winform工程\bin\目录下 创建日志文件夹 

                if (Directory.Exists(strPath) == false)                          //工程目录下 Log目录 '目录是否存在,为true则没有此目录
                {
                    Directory.CreateDirectory(strPath);                       //建立目录　Directory为目录对象
                }
                strPath = strPath + "\\" + dt.ToString("yyyy");

                if (Directory.Exists(strPath) == false)
                {
                    Directory.CreateDirectory(strPath);
                }
                strPath = strPath + "\\" + dt.Year.ToString() + "-" + dt.Month.ToString() + ".txt";

                StreamWriter FileWriter = new StreamWriter(strPath, true);           //创建日志文件
                FileWriter.WriteLine(string.Format("[{0}] [Error] [引发异常的方法]:{1} [异常堆栈信息]:{2} [异常消息]:{3}", dt.ToString("yyyy-MM-dd HH:mm:ss"), msg.TargetSite, msg.StackTrace, msg.Message));
                FileWriter.Close();                                                 //关闭StreamWriter对象
            }
            catch (Exception ex)
            {
                string str = ex.Message.ToString();
            }
        }
    }
}
