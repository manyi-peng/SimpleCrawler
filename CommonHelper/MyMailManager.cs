using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll
{
    public class MyMailManager
    {
        private static readonly string rootdir = System.IO.Directory.GetCurrentDirectory();
        private string _mailFrom = System.Configuration.ConfigurationManager.AppSettings["mailFrom"].ToString();
        private string _mailPwd = System.Configuration.ConfigurationManager.AppSettings["mailPwd"].ToString();
        private string _host = System.Configuration.ConfigurationManager.AppSettings["host"].ToString();

        #region 字段
        /// <summary>
        /// 发送者
        /// </summary>
        public string mailFrom { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string[] mailToArray { get; set; }

        /// <summary>
        /// 抄送
        /// </summary>
        public string[] mailCcArray { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string mailSubject { get; set; }

        /// <summary>
        /// 正文
        /// </summary>
        public string mailBody { get; set; }

        /// <summary>
        /// 发件人密码
        /// </summary>
        public string mailPwd { get; set; }

        /// <summary>
        /// SMTP邮件服务器
        /// </summary>
        public string host { get; set; }

        /// <summary>
        /// 正文是否是html格式
        /// </summary>
        public bool isbodyHtml { get; set; }

        /// <summary>
        /// 附件
        /// </summary>
        public string[] attachmentsPath { get; set; }

        #endregion

        public bool Send()
        {

            try
            {
                //使用指定的邮件地址初始化MailAddress实例
                MailAddress maddr = new MailAddress(mailFrom);
                //初始化MailMessage实例
                MailMessage myMail = new MailMessage();


                //向收件人地址集合添加邮件地址
                if (mailToArray != null)
                {
                    for (int i = 0; i < mailToArray.Length; i++)
                    {
                        myMail.To.Add(mailToArray[i].ToString());
                    }
                }

                //向抄送收件人地址集合添加邮件地址
                if (mailCcArray != null)
                {
                    for (int i = 0; i < mailCcArray.Length; i++)
                    {
                        myMail.CC.Add(mailCcArray[i].ToString());
                    }
                }
                //发件人地址
                myMail.From = maddr;

                //电子邮件的标题
                myMail.Subject = mailSubject;

                //电子邮件的主题内容使用的编码
                myMail.SubjectEncoding = Encoding.UTF8;

                //电子邮件正文
                myMail.Body = mailBody;

                //电子邮件正文的编码
                myMail.BodyEncoding = Encoding.Default;

                myMail.Priority = MailPriority.Normal;

                myMail.IsBodyHtml = isbodyHtml;

                //在有附件的情况下添加附件
                try
                {
                    if (attachmentsPath != null && attachmentsPath.Length > 0)
                    {
                        Attachment attachFile = null;
                        foreach (string path in attachmentsPath)
                        {
                            attachFile = new Attachment(path);
                            //设置附件名为原始文件名
                            attachFile.ContentDisposition.FileName = System.IO.Path.GetFileName(path);
                            myMail.Attachments.Add(attachFile);
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception("在添加附件时有错误:" + err);
                }

                SmtpClient smtp = new SmtpClient();
                //指定发件人的邮件地址和密码以验证发件人身份
                smtp.Credentials = new System.Net.NetworkCredential(mailFrom, mailPwd);


                //设置SMTP邮件服务器
                smtp.Host = host;

                try
                {
                    //将邮件发送到SMTP邮件服务器
                    smtp.Send(myMail);
                    smtp.Dispose();
                    return true;

                }
                catch (System.Net.Mail.SmtpException)
                {
                    return false;
                }
                finally
                {
                    //释放smtp资源
                    smtp.Dispose();
                }
            }
            catch (Exception ex)
            {
                //记录日志
                MyCommonManager.ErrorLog(ex, "sendMail-Ex", rootdir);
                return false;
            }
        }


        //设置邮件信息
        public void SetMailMessageAndSend(string msg, string[] myMailToArray)
        {
            MyMailManager myMailManager = new MyMailManager();
            myMailManager.mailFrom = _mailFrom;
            myMailManager.mailPwd = _mailPwd;
            DateTime now = DateTime.Now;
            //邮件主题
            string myMailSubject = string.Format("服务异常通知");
            //邮件内容
            myMailManager.mailSubject = myMailSubject;
            string myMailBody = string.Format(
                "<p style=\"padding:0px;margin:0px;line-height:24px;\">Dear All:</p><br/><p style=\"padding:0px;margin:0px;line-height:24px;\">{0}</p><br/>" +
                "<p style=\"padding:0px;margin:0px;line-height:24px;\">" +
                "Best regards</p><p style=\"padding:0px;margin:0px;line-height:24px;\">高新区智慧生活 {1}</p>" +
                "<p style=\"padding:0px;margin:0px;line-height:24px;\"></p>",
                msg,now.AddDays(-1).ToString("yyyy年MM月dd日"));
            myMailManager.mailBody = myMailBody;
            myMailManager.isbodyHtml = true;
            myMailManager.host = _host;

            myMailManager.mailToArray = myMailToArray;

            //发送邮件
            myMailManager.Send();
        }


    }
}
