//#if DNXCORE50 || DOTNET5_5

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
//using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SqCommon
{
    // Option 1.
    //Mail support is not trivial, they are working on it.
    //There is an SSH example too here
    //https://github.com/dotnet/corefx/issues/1006
    //"And here is a sample for wrapping an SMTP connection into an sslStream for secure mail to places like Gmail."
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
    public class Email
    {
        public string ToAddresses;
        public string Subject;
        public string Body;
        public bool IsBodyHtml;

        public static string SenderName;
        public static string SenderPwd;

        public void Send()
        {
            //if (Utils.RunningPlatform() == Platform.Linux)
            //    SendLinuxCommandLine();
            //else
            SendWithSystemNetSecuritySslStream();   // "\r\n" for non-Unix platforms
        }


        internal void SendLinuxCommandLine()
        {
            //Utils.Logger.Info("HQEmail.SendLinuxCommandLine(). Subject: " + Subject + " Body: " + Body);
            //You should use the -c option to execute a command.Furthermore, you should quote the command itself so that it gets passed as a single argument. Something like this should work:
            //var processStartInfo = new ProcessStartInfo { FileName = "/bin/bash", Arguments = "-c \"echo test | sudo -S shutdown -r +1\"" };

            // my command: echo "Body" > email_11.txt && mail -s "My-subject" "ToAddresses" < email_11.txt
            //my command: echo "Body" > email_11.txt && mail -s "My-subject" -r "ToAddresses" < email_11.txt
            //ProcessStartInfo procStartInfo = new ProcessStartInfo("/bin/bash", @"-c ls");
            // Error: Don't write hyphen ('-') into the Subject, because it thinks, it is another parameter.  
            // and mail gives this error message: "Send options without primary recipient specified."
            //string argumentsStr = "-c \"echo \"Body\" > email_11.txt && mail -s \"MySubject\" \"ToAddresses\" < email_11.txt\"";
            // temporarily only works, if there is no space in the body
            //string argumentsStr = "-c \"echo \"This is message body from Linux\" | mail -s \"This is Subject1 from Linux\" \"ToAddresses\"\"";  
            //string argumentsStr = "-c \"echo \"This_is_message_body_from_Linux_Temporary_No_space\" | mail -s \"This_is_Subject1_from_Linux\" \"ToAddresses\"\"";
            // use ' instead of " and you can have space in the text too new ProcessStartInfo("/bin/bash", argumentsStr);
            //string argumentsStr = "-c \"echo 'This is message body from Linux Temporary (with_space_working)' | mail -s 'This is Subject1 from Linux command line' \"" + ToAddresses + "\"\"";
            string argumentsStr;

            // (`) can be in the MessageBody, like this "StackTrace:    at System.Threading.Tasks.Task`1.GetResultCore(Boolean waitCompletionNotification)", 
            // however, bash doesn't like it.  "/bin/bash: -c: line 0: syntax error near unexpected token `('", so eliminate it from both HTML and non-html versions.

            string preparedBody = Body.Replace("`", "'");

            if (IsBodyHtml)
            {
                //http://stackoverflow.com/questions/2591755/how-send-html-mail-using-linux-command-line        // this doesn't work for me, because my mailx is Heirloom and -a means attachment
                //argumentsStr = "-c \"echo '" + Body + "' | mail -a 'Content-type: text/html' -s '" + Subject + "' \"" + ToAddresses + "\"\"";
                //                string argumentsStrTest =
                //@"-c ""(
                //echo """"From: me@xyz.com """"
                //echo """"To: gyantal@gmail.com """"
                //echo """"MIME-Version: 1.0""""
                //echo """"Content-Type: multipart/alternative\; """" 
                //echo ' boundary=""""""""some.unique.value.ABC123/server.xyz.com""""""""' 
                //echo """"Subject: Test HTML e-mail."""" 
                //echo """""""" 
                //echo """"This is a MIME-encapsulated message"""" 
                //echo """""""" 
                //echo """"--some.unique.value.ABC123/server.xyz.com"""" 
                //echo """"Content-Type: text/html"""" 
                //echo """""""" 
                //echo """"\<html\>"""" 
                //echo """"\<head\>"""" 
                //echo """"\<title\>HTML E-mail\</title\>"""" 
                //echo """"\</head\>"""" 
                //echo """"\<body\>"""" 
                //echo """"\<a href=\'http://www.google.com\'\>Click Here\</a\>"""" 
                //echo """"\</body\>"""" 
                //echo """"\</html\>"""" 
                //echo """"------some.unique.value.ABC123/server.xyz.com--"""" 
                //) | sendmail -t""";

                // see "myknowledge\Linux\OS\Sending Html email from Bash.txt" and we make one big line of Body. Other option is that keep many lines, but write 'echo' in front of them.
                string preparedHtmlBody = preparedBody.Replace("\"", "\"\"").Replace(@"<", @"\<").Replace(@">", @"\>").Replace(@";", @"\;").Replace(@"'", @"\'").Replace(@"(", @"\(").Replace(@")", @"\)").Replace(@"{", @"\{").Replace(@"}", @"\}").Replace(@"#", @"\#").Replace("\n", "").Replace("\r", "");

                argumentsStr =
                @"-c ""(
echo """"From: me@xyz.com """"
echo """"To: " + ToAddresses + @" """"
echo """"MIME-Version: 1.0""""
echo """"Content-Type: multipart/alternative\; """" 
echo ' boundary=""""""""some.unique.value.ABC123/server.xyz.com""""""""' 
echo """"Subject: " + Subject + @""""" 
echo """""""" 
echo """"This is a MIME-encapsulated message"""" 
echo """""""" 
echo """"--some.unique.value.ABC123/server.xyz.com"""" 
echo """"Content-Type: text/html"""" 
echo """""""" 
echo """"" + preparedHtmlBody + @""""" 
) | sendmail -t""";
                argumentsStr = argumentsStr.Replace("\r", "");  // bash doesn't like new lines, like \r. error was: /bin/bash: $'\r': command not found
            }
            else
            {
                // /bin/bash: -c: line 1: syntax error near unexpected token `(' , but when the same message was sent as Html, it worked
                if (preparedBody.IndexOf("\r") != -1)
                    Utils.ConsoleWriteLine(ConsoleColor.Red, "Warning. Linux bash doesn't like NewLines. NewLines can be removed from HTML emails, but not nicely from Text emails. Use IsBodyHtml=True.");

                argumentsStr = "-c \"echo '" + preparedBody + "' | mail -s '" + Subject + "' \"" + ToAddresses + "\"\"";
            }
            Utils.Logger.Info("HQEmail.SendLinuxCommandLine() bash command arguments: " + argumentsStr);
            ProcessStartInfo procStartInfo = new ProcessStartInfo("/bin/bash", argumentsStr);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            string result = proc.StandardOutput.ReadToEnd();
            if (!String.IsNullOrEmpty(result))
                Utils.Logger.Error("Executed bash result (Empty means OK. Error if it is not empty): " + result);
        }


        //********************   smtp.gmail.com Error.
        //This was received as error on Linux SSL email-sending:
        //"535-5.7.8 Username and Password not accepted. Learn more at
        //535 5.7.8  https://support.google.com/mail/answer/14257 k6sm538174qkc.42 - gsmtp"
        //From ">-crlf: this option translated a line feed from the terminal into CR+LF as required by some servers.
        //" I found out that CR + LF was the problem.
        //After: secureWriter.Write(base64Username + myNewLine); , where myNewLine = "\r\n".It works now.
        string myNewLine = "\r\n"; // Environment.NewLine : A string containing "\r\n" for non-Unix platforms, or a string containing "\n"

        internal async void SendWithSystemNetSecuritySslStream()
        {
            Console.WriteLine("Email SendWithSystemNetSecuritySslStream()");
            string outputString = "";

            try
            {
                const string server = "smtp.gmail.com";
                //const int port = 465;
                const int port = 587;
                //const int port = 25;

                using (var client = new TcpClient())
                {
                    //Console.WriteLine("[Client] Attempting to Connect to server");
                    //await client.ConnectAsync(server, port);
                    Task connectTask = client.ConnectAsync(server, port);
                    await connectTask;
                    //connectTask.Wait();
                    //Task timeoutTask = Task.Delay(millisecondsDelay: 10000);
                    //if (Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    //{
                    //    throw new TimeoutException();
                    //}
                    //Console.WriteLine("[Client] Connected to server");
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        outputString += "Server-1: " + reader.ReadLine() + Environment.NewLine;

                        writer.Write("HELO " + server + myNewLine);
                        outputString += "Client: " + "HELO " + server + Environment.NewLine;
                        outputString += "Server-2: " + reader.ReadLine() + Environment.NewLine;

                        writer.Write("STARTTLS" + myNewLine);
                        outputString += "Client: " + "STARTTLS" + Environment.NewLine;
                        outputString += "Server-3: " + reader.ReadLine() + Environment.NewLine;

                        // maybe on Linux this SslStream is not a real SslStream(), and somehow I got no error about it
                        using (var sslStream = new SslStream(client.GetStream(), false))
                        {
                            //sslStream.AuthenticateAsClient(server);
                            //sslStream.AuthenticateAsClientAsync(server).Wait();
                            await sslStream.AuthenticateAsClientAsync(server);

                            //X509Certificate2Collection xc = new X509Certificate2Collection();
                            //sslStream.AuthenticateAsClientAsync(server, xc, SslProtocols. Security.Authentication.SslProtocols.Tls, false);
                            //sslStream.AuthenticateAsClientAsync(server, xc, SslProtocols.Tls, false).Wait();


                            using (var secureReader = new StreamReader(sslStream))
                            using (var secureWriter = new StreamWriter(sslStream) { AutoFlush = true })
                            {

                                secureWriter.Write("AUTH LOGIN" + myNewLine);
                                outputString += "Client: " + "AUTH LOGIN" + Environment.NewLine;
                                //secureWriter.WriteLine("AUTH PLAIN");
                                //outputString += "Client: " + "AUTH PLAIN" + Environment.NewLine;
                                outputString += "Server-4: " + secureReader.ReadLine() + Environment.NewLine;

                                var plainTextBytes1 = System.Text.Encoding.UTF8.GetBytes(SenderName);
                                string base64Username = System.Convert.ToBase64String(plainTextBytes1);
                                secureWriter.Write(base64Username + myNewLine);
                                outputString += "Client: " + base64Username + Environment.NewLine;
                                outputString += "Server-5: " + secureReader.ReadLine() + Environment.NewLine;

                                var plainTextBytes2 = System.Text.Encoding.UTF8.GetBytes(SenderPwd);
                                string base64Password = System.Convert.ToBase64String(plainTextBytes2);
                                secureWriter.Write(base64Password + myNewLine);
                                outputString += "Client: " + base64Password + Environment.NewLine;
                                string authReply = secureReader.ReadLine();
                                outputString += "Server-6: " + authReply + Environment.NewLine;
                                if (authReply.ToLower().IndexOf("not accepted", 0) != -1)
                                {
                                    Console.WriteLine("Not accepted is found in " + authReply + ". The OutputString: " + outputString);
                                    throw new Exception("ERROR. Google server says: '" + authReply + "'");
                                    //return; // Don't throw exceptions, because Linux hates it. Console is going havoc.
                                }

                                secureWriter.Write("MAIL FROM:<" + SenderName + ">" + myNewLine);
                                outputString += "Client: " + "MAIL FROM:<" + SenderName + ">" + Environment.NewLine;
                                outputString += "Server-7: " + secureReader.ReadLine() + Environment.NewLine;

                                //http://www.samlogic.net / articles / smtp - commands - reference.htm
                                // This command can be repeated multiple times for a given e-mail message in order to deliver a single e-mail message to multiple recipients. 
                                secureWriter.Write("RCPT TO:<" + ToAddresses + ">" + myNewLine);
                                outputString += "Client: " + "RCPT TO:<" + ToAddresses + ">" + Environment.NewLine;
                                outputString += "Server-8: " + secureReader.ReadLine() + Environment.NewLine;

                                secureWriter.Write("DATA" + myNewLine);
                                outputString += "Client: " + "DATA" + Environment.NewLine;
                                outputString += "Server-9: " + secureReader.ReadLine() + Environment.NewLine;

                                secureWriter.Write("From: " + SenderName + myNewLine);
                                outputString += "Client: " + "From: " + SenderName + Environment.NewLine;
                                secureWriter.Write("To:  " + ToAddresses + myNewLine);
                                outputString += "Client: " + "To:  " + ToAddresses + Environment.NewLine;
                                secureWriter.Write("Subject: " + Subject + myNewLine);
                                outputString += "Client: " + "Subject: " + Subject + Environment.NewLine;
                                if (IsBodyHtml)
                                {
                                    // https://blogs.msdn.microsoft.com/mim/2013/11/29/sending-an-email-within-a-windows-8-1-application-using-streamsocket-to-emulate-a-smtpclient/
                                    secureWriter.Write("Content-Type: text/html; " + myNewLine);
                                    outputString += "Client: " + "Content-Type: text/html; " + Environment.NewLine;
                                }

                                // Leave one blank line after the subject
                                secureWriter.Write("" + myNewLine);
                                outputString += "Client: " + "" + Environment.NewLine;
                                // Start the message body here
                                secureWriter.Write(Body + myNewLine);
                                outputString += "Client: " + Body + Environment.NewLine;
                                //secureWriter.WriteLine("Hello Luke,");
                                //secureWriter.WriteLine("");
                                //secureWriter.WriteLine("Cuz! You gotta try Beck's Sapphire! It ROCKS!");
                                //secureWriter.WriteLine("");
                                //secureWriter.WriteLine("Later,");
                                //secureWriter.WriteLine("");
                                //secureWriter.WriteLine("Luke");
                                // End the message body by sending a period
                                secureWriter.Write("." + myNewLine);
                                outputString += "Client: " + "." + Environment.NewLine;
                                outputString += "10: " + secureReader.ReadLine() + Environment.NewLine;

                                secureWriter.Write("QUIT" + myNewLine);
                                //Note, some hosts may require a "<CRLF>.<CRLF>" ending, instead of the proposed ". QUIT".
                                //writer.WriteLine("\r\n.\r\n");
                                outputString += "Server-11: " + secureReader.ReadLine() + Environment.NewLine;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("*Exception Message" + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("*Inner: " + ex.InnerException.Message);
                }
                throw;
            }


            //Console.WriteLine(outputString);
            Utils.Logger.Info(outputString);

            Console.WriteLine("HQEmail.Send() END");
        } //~ HQEmail

    }
}
//#endif

