using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreTestNetSecurity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //EncryptionPolicy policy = EncryptionPolicy.AllowNoEncryption;
            //Console.WriteLine("Something from System.Net.Security: " + policy.ToString());

            Console.WriteLine("*** Hello DotNetCoreTestNetSecurity! This test will try to send 1 email on Windows (based on System.Net.Security.SslStream) and 2 emails on Linux. First email is sent with command line shell script, second is sent with System.Net.Security.SslStream.");
            Console.WriteLine("*** Previous problems: 4.0.0-rc3-23728 (2016-01-29): 'Server - 6: 535 - 5.7.8 Username and Password not accepted.'");
            Console.WriteLine("*** Previous problems: 4.0.0-rc3-23808 (2016-02-09): 'Unable to find an entry point named 'GlobalizationNative_ToAscii' in DLL 'System.Globalization.Native''");

            string toAddress;

            try
            {
                string configWinPath = @"g:\agy\Google Drive\GDriveHedgeQuant\shared\GitHubRepos\NonCommitedSensitiveData\SQTestNetSecurityNoGitHubConfig.txt";
                string configLinuxPath = @"/home/ubuntu/SQ/Server/Test/TestNetSecurity/SQTestNetSecurityNoGitHubConfig.txt";
                string configPath = String.Empty;
                if (File.Exists(configWinPath))
                    configPath = configWinPath;
                else if (File.Exists(configLinuxPath))
                    configPath = configLinuxPath;
                else
                {
                    Console.WriteLine("*** SQTestNetSecurityNoGitHubConfig.txt is not found. Program exits.");
                    Environment.Exit(0);
                }

                string[] lines = System.IO.File.ReadAllLines(configPath);
                HQEmail.SenderName = lines[0];
                HQEmail.SenderPwd = lines[1];
                toAddress = lines[2];
            }
            catch (Exception)
            {
                Console.WriteLine("");
                throw;
            }


            var email = new HQEmail { ToAddresses = toAddress, Subject = "Test from DotNetCoreTestNetSecurity", Body = "This is a test.", IsBodyHtml = false };

            try
            {
                // 4.0.0-rc3-23728 (2016-01-29): 'Server-6: 535-5.7.8 Username and Password not accepted.', because SslStream doesn't send it.
                // 4.0.0-rc3-23808 (2016-02-09): 'Unable to find an entry point named 'GlobalizationNative_ToAscii' in DLL 'System.Globalization.Native'', 
                // because this guy  ruined it on 30th January: https://github.com/dotnet/corefx/blob/4fedb72e274686cbe523261ae3f7b7ae9e2e314d/src/Common/src/Interop/Unix/System.Globalization.Native/Interop.Idna.cs
                if (Environment.NewLine == "\n")
                {
                    email.Subject = "Test from DotNetCoreTestNetSecurity, Linux command line.";
                    email.SendLinuxCommandLine(true);
                    Console.WriteLine("*** 1. email.SendLinuxCommandLine() seemed succesfull.");

                    email.Subject = "Test from DotNetCoreTestNetSecurity, Linux SslStream.";
                    email.SendWithSystemNetSecuritySslStream(true);
                    Console.WriteLine("*** 2. IF there was no Error message: BINGO!!!! on Linux. email.SendWithSystemNetSecuritySslStream() seemed succesfull. It seems SslStream bug in DotNetCore is fixed. Rewrite email sending in SQLab Utils.");
                }
                else
                {
                    email.Subject = "Test from DotNetCoreTestNetSecurity, Windows SslStream.";
                    email.SendWithSystemNetSecuritySslStream(true);  // "\r\n" for non-Unix platforms
                    Console.WriteLine("*** on Windows: email.SendWithSystemNetSecuritySslStream() seemed succesfull. We expected that.");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("*** Too bad. Exception occured. They haven't fixed the SslStream issue in DotNetCore yet. Try again later.");
                throw;
            }



            Console.WriteLine("*** To exit program: type something and press Enter.");

            Console.ReadLine();
        }
    }
}
