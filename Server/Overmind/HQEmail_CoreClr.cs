#if DNXCORE50

using System;

namespace Overmind
{
    // Option 1.
    //Mail support is not trivial, they are working on it.
    //There is an SSH example too here
    //https://github.com/dotnet/corefx/issues/1006
    //that can send SSL secure email too. In .NET Core, if I make my own MailClient.
    //One final example in case the mail server doesn't support TLS but does support SSL (port 465), such as GoDaddy :toilet: mail servers :poop:.
    //const string server = "smtpout.secureserver.net";
    //const int port = 465;
    //using (var client = new TcpClient(server, port)) {
    //using (var stream = new SslStream(client.GetStream(), false)) {

    // Option 2 (Better)
    //or jstedfast commented on Dec 5, 2015
    //For anyone interested in MailMessage / SmtpClient equivalent support, I've just finished porting MimeKit and MailKit over to CoreCLR.
    // nugets are available now.
    //Thanks @jstedfast - Really appreciate your contribution here! We will continue to keep System.Net.Mail for .NET Core in our plans, but it's really nice to see .NET Core developers unblocked thanks to you :)
    public class HQEmail
    {
        public string ToAddresses;
        public string Subject;
        public string Body;
        public bool IsBodyHtml;

        internal void Send(bool p_enableSsl)
        {


        } //~ HQEmail

    }
}
#endif

