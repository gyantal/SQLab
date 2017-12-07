using SqCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YahooFinanceAPI
{
    /// <summary>
    /// Class for fetching token (cookie and crumb) from Yahoo Finance
    /// Copyright Dennis Lee
    /// 19 May 2017
    /// https://github.com/dennislwy/YahooFinanceAPI
    /// </summary>
    public class Token
    {
        public static string Cookie { get; set; }       // cookie expires in 1 year: response.Headers["Set-Cookie"]	"B=fb0rbqhci8524&b=3&s=7b; expires=Tue, 23-May-2018 10:51:48 GMT; path=/; domain=.yahoo.com"	string


        private static string m_crumb;
        public static string Crumb
        {
            get
            {
                if (CrumbAcquireTime < DateTime.UtcNow.AddHours(-12))   // we don't know when crumb expires. Assume it expires every 12 hours.
                                                                        //if (CrumbAcquireTime < DateTime.UtcNow.AddSeconds(-5))   // we don't know when crumb expires. Assume it expires every 12 hours.
                    return null;
                return m_crumb;
            }
            set
            {
                m_crumb = value;
                if (!String.IsNullOrEmpty(m_crumb))
                    CrumbAcquireTime = DateTime.UtcNow;
            }
        }

        public static DateTime CrumbAcquireTime { get; set; } = DateTime.MinValue;

        private static Regex regex_crumb;

        /// <summary>
        /// Refresh cookie and crumb value
        /// </summary>
        /// <param name="symbol">Stock ticker symbol</param>
        /// <returns></returns>
        public static bool Refresh(string symbol = "SPY")
        {

            try
            {
                Token.Cookie = "";
                Token.Crumb = "";

                string url_scrape = "https://finance.yahoo.com/quote/{0}?p={0}";
                //url_scrape = "https://finance.yahoo.com/quote/{0}/history"

                string url = string.Format(url_scrape, symbol);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

                request.CookieContainer = new CookieContainer();
                request.Method = "GET";

                var task = request.GetResponseAsync();
                task.Wait(10000); // Blocks current thread until GetFooAsync task completes; timeout is 10000msec
                using (var response = (HttpWebResponse)task.Result)
                {
                    // response.Headers["Set-Cookie"]	"B=fb0rbqhci8524&b=3&s=7b; expires=Tue, 23-May-2018 10:51:48 GMT; path=/; domain=.yahoo.com"	expires in 1 year
                    string cookie = response.Headers["Set-Cookie"].Split(';')[0];
                    string html = "";

                    using (Stream stream = response.GetResponseStream())
                    {
                        //var sr = new StreamReader(stream, Encoding.UTF7);   // I have tried many encodings, UTF7, UTF8, Unicode, ASCII, the  "\u002F" is always in the html. Maybe .NET core problem and will be fixed in .NET Core 2.0
                        var sr = new StreamReader(stream, true);
                        html = sr.ReadToEnd();
                    }

                    if (html.Length < 5000)
                        return false;
                    string crumb = getCrumb(html);
                    html = "";

                    if (crumb != null)
                    {
                        Token.Cookie = cookie;
                        Token.Crumb = crumb;
                        //Debug.Print("Crumb: '{0}', Cookie: '{1}'", crumb, cookie);
                        Utils.Logger.Info(String.Format("Crumb: '{0}', Cookie: '{1}'", crumb, cookie));
                        return true;
                    }
                }

            }
            catch (Exception ex)
            {
                //Debug.Print(ex.Message);
                Utils.Logger.Error(ex, "Exception in Token.cs");
            }

            return false;

        }

        /// <summary>
        /// Get crumb value from HTML
        /// </summary>
        /// <param name="html">HTML code</param>
        /// <returns></returns>
        private static string getCrumb(string html)
        {

            string crumb = null;

            try
            {
                //initialize on first time use;  html has this 
                //""CrumbStore":{"crumb":"nPZ6EeAe4Xq"},", "CrumbStore":{"crumb":"TuEY..4hqHq"}, "CrumbStore":{"crumb":"Kr32X0\u002Fg5\u002Fs"}  '\u002F' = forward slash '/'
                if (regex_crumb == null)
                    //regex_crumb = new Regex("CrumbStore\":{\"crumb\":\"(?<crumb>\\w+)\"}",    // this accepts only word characters
                    regex_crumb = new Regex("CrumbStore\":{\"crumb\":\"(?<crumb>[^\"]+)\"}",    // this accepts anything buy '"' in the crumb, like \ or (.) dots
                        RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

                MatchCollection matches = regex_crumb.Matches(html);
                if (matches.Count > 0)
                {
                    string rawCrumb = matches[0].Groups["crumb"].Value;     // "178n9nvI\\u002FeI"  , actually, if I convert to  ToCharArray(), there is only 1 '\' in it.

                    // '\u002F' has to be converted to forward slash '/'. If not "The remote server returned an error: (401) Unauthorized."
                    // tried many ways to convert \u002f back to '/' called unescape-ing the escaped unicode; 
                    // some people suggested Regex.Replace or Regex.Unescape, but I hate Regex, because it is slow
                    // https://stackoverflow.com/questions/8558671/how-to-unescape-unicode-string-in-c-sharp
                    // so I just quickly replace it withou Regex
                    crumb = rawCrumb.Replace(@"\u002F", @"/");
                    Utils.Logger.Info("Raw crumb found: '" + rawCrumb + "', Corverted: '" + crumb + "'");
                }
                else
                {
                    //Debug.Print("Regex no match");
                    Utils.Logger.Error("Error in getCrumb(). Regex no match.");
                }

                //prevent regex memory leak
                matches = null;

            }
            catch (Exception ex)
            {
                Utils.Logger.Error(ex, "Exception in Token.cs");
                //Debug.Print(ex.Message);
            }

            GC.Collect();
            return crumb;

        }

    }
}