using BPS.EdOrg.Loader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader.MetaData
{
    class Notification
    {
        
        private readonly string _recipient;
        private readonly string _subject;
        private readonly string _body;
        private readonly string _attachmentFilename;

        public Notification(string recipient, string subject, string body, string attachmentFilename)
        {
            _recipient = recipient;
            _subject = subject;
            _body = body;
            _attachmentFilename = attachmentFilename;
        }
        /// <summary>
        /// Sending the log in the email
        /// </summary>
        /// <returns></returns>
        public bool SendMail(string recipient, string subject, string body, string attachmentFilename)
        {
            try
            {
                SendEmail emailObj = new SendEmail();
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    Attachment att = null;
                    if (File.Exists(Constants.LOG_FILE))
                    {
                        att = new Attachment(Constants.LOG_FILE);
                        emailObj.AttachmentList = new List<Attachment> { att };
                    }
                    emailObj.ToAddr = new System.Collections.ArrayList();
                    emailObj.ToAddr.Add(recipient);
                    emailObj.FromAddr = Constants.EmailFromAddress;
                    emailObj.EmailSubject = subject;
                    emailObj.EmailContent = body;
                    SendEmailNotification(emailObj);
                }
                return true;
            }

            catch (Exception ex)
            {
                string message = $"Exception while sending email " + ex;
                //new EmailException(message, ex);
                return false;
            }

        }


        /// <summary>
        /// Send & Save email notification - general 
        /// </summary>
        /// <param name="emailObj"></param>
        /// <returns></returns>
        private bool SendEmailNotification(SendEmail emailObj)
        {

            MailMessage message = new MailMessage();
            message.From = new MailAddress(emailObj.FromAddr);
            foreach (string toString in emailObj.ToAddr)
            {
                message.To.Add(toString);
            }

            //send if there are attachments
            if (emailObj.AttachmentList != null && emailObj.AttachmentList.Count > 0)
            {
                foreach (Attachment att in emailObj.AttachmentList)
                {
                    message.Attachments.Add(att);
                }
            }

            if (!String.IsNullOrEmpty(emailObj.BccToAdr))
            {
                message.Bcc.Add(new MailAddress(emailObj.BccToAdr));
            }

            message.Subject = emailObj.EmailSubject;
            message.IsBodyHtml = true;
            AlternateView av = AlternateView.CreateAlternateViewFromString(emailObj.EmailContent, null, MediaTypeNames.Text.Html);
            message.AlternateViews.Add(av);
            using (SmtpClient smtp = new SmtpClient())
            {
                smtp.Host = Constants.SmtpServerHost;
                smtp.Send(message);
                return true;
            }
        }
    }
}
