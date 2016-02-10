#if !DNXCORE50

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Overmind
{
    public class HQEmail
    {
        public string ToAddresses;
        public string Subject;
        public string Body;
        public bool IsBodyHtml;

        public static string SenderName;
        public static string SenderPwd;

        //const string aTo = "To", aSubject = "Subject", aIsBodyHtml = "IsBodyHtml";
        //const string tBody = "Body", tAttachment = "Attachment", tEmail = "HQEmail";


        //**************** Solution to made Mono Smtp email with Google work:
        //-SSH is required by Google.
        //-Mono doesn't trust Google Server or anybody,
        //1.
        //sudo mozroots --import --ask-remove --machine           // did the machine version
        //sudo certmgr -ssl smtps://smtp.gmail.com:465			// used 465 port, even though email is send
        //2.
        //I logged in to that account in Chrome(using same IP) in LXDE
        //3.
        //Settings/Pop was not enabled for a**666@gm**l.com(that was the reason it didn't work)
        internal void SendOnMono(bool p_enableSsl)
        {
          /*  MailMessage mail = new MailMessage();

            mail.From = new MailAddress(SenderName);
            mail.To.Add(Controller.g_controller.g_configuration.GetSection("EmailGyantal").Value);
            mail.Subject = "Test Mail";
            mail.Body = "This is for testing SMTP mail from GMAIL";

            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new System.Net.NetworkCredential(SenderName, SenderPwd);
            smtpServer.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; };
            smtpServer.Send(mail); */

        }

        // works under Mono, the X509Certificate certificate thing is not required
        internal void Send(bool p_enableSsl)
        {
            Console.WriteLine("HQEmail.Send(), nonCoreClr BEGIN... but it is commented out.");
            Program.gLogger.Info("HQEmail.Send(), nonCoreClr BEGIN... but it is commented out.");


/*
            using (MailMessage message = new MailMessage())
            {
                foreach (string toAddress in ToAddresses.Split(new char[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    message.To.Add(new System.Net.Mail.MailAddress(toAddress));
                }
                Body = Body ?? String.Empty;
                if (message.To.Count == 0)
                {
                    throw new Exception("No addressee");
                }

                message.From = new System.Net.Mail.MailAddress(SenderName); // gmail will not use this, because it authenticates the user 'HedgeQuantServer'
                message.Subject = Subject ?? String.Empty;
                message.IsBodyHtml = IsBodyHtml;
                message.Body = Body;

                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
                smtpClient.Port = 587;  // later: try 25, 465, 475, 587 if the default 587 doesn't work // Port: 465 or 587 see http://j.mp/rZ9l6I
                // Google requires SSL, so I cannot disable it.
                smtpClient.EnableSsl = p_enableSsl;    // to reduce SmtpExceptions (still remains some) ("The SMTP server requires a secure connection or the client was not authenticated")
                //smtpClient.UseDefaultCredentials = false;   // do not use current windows username/pwd. (The default would be 'false', too)
                smtpClient.Credentials = new NetworkCredential(SenderName, SenderPwd);    // 'username' should end with @gmail.com (according to http://j.mp/rZ9l6I and http://j.mp/t1P6WK)
                smtpClient.Timeout = 120000;    // default is 100 000 msec (but it was timed out once)

                // exceptions are handled in the caller SafeSendEmail()
                smtpClient.Send(message);
                //Utils.Logger.Info("{0}: Email message was sent", Utils.GetCurrentMethodName());

                //if (new Random().Next(10) >= 4)
                //    throw new Exception("Testing exception handling in " + Utils.GetCurrentMethodName());
            }
            Program.gLogger.Info("HQEmail.Send() END"); */
        }


    } //~ HQEmail

}
#endif

