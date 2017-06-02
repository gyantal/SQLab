using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Text;
using SqCommon;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using YahooFinanceAPI;
using System.IO;
using Microsoft.Extensions.Primitives;

//// Future work: after Robert confirmed that v8 (without crumbs) gives same good data as v7 (with crumbs), we may change implementation to that  (maybe not, as it gives slightly different data)
//egy hasonló már le van implementálva a programban,  csak    query1.finance.yahoo.com/v7/finance/download/... helyett query2.finance.yahoo.com/v8/finance/chart/...  lásd itt:
//https://incode.browse.cloudforge.com/cgi-bin/hedgequant/HedgeQuant/src/Server/YahooQuoteCrawler/Crawler.cs?revision=7881&view=markup#l1612
//és ehhez nem kell se crumbs, se cookie a tapasztalatom szerint, és mint mondtam sok benne az adathiba.
//Majd valamikor megnézem hogy a query1.finance.yahoo.com/v7/-es API jobb adatokat ad-e mint a query2.finance.yahoo.com/v8/-as,
//de ez egy hosszabb nekigyűrkőzést igénylő munka/vizsgálódás.

// https://www.snifferquant.net/YahooFinanceForwarder?yffOutFormat=json&yffColumns=dc1&jsonp=JsonpCallbackFunc&yffUri=query1.finance.yahoo.com/v7/finance/download/%5EVIX&period1=1990-01-02&period2=UtcNow&interval=1d&events=history
namespace SQLab.Controllers
{
    //[Route("api/[controller]")]
    public class YahooFinanceForwarder : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public YahooFinanceForwarder(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

#if !DEBUG
        [Authorize]
#endif
        public ActionResult Index()
        {
            var authorizedEmailResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); if (authorizedEmailResponse != null) return authorizedEmailResponse;

            Tuple<string, string> contentAndType = GenerateYffResponse();
            return Content(contentAndType.Item1, contentAndType.Item2);

        }

        private Tuple<string, string> GenerateYffResponse()
        {
            try
            {
                // 1. Prepare input parameters
                string uriQuery = Request.QueryString.ToString();    // "?s=VXX,XIV,^vix&f=ab&o=csv" from the URL http://localhost:58213/api/rtp?s=VXX,XIV,^vix&f=ab&o=csv
                if (uriQuery.Length > 8192)
                {//When you try to pass a string longer than 8192 charachters, a faultException will be thrown. There is a solution, but I don't want
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error caught by WebApi Get():: uriQuery is longer than 8192: we don't process that. Uri: " + uriQuery + @""" }", "application/json");
                }

                Dictionary<string, StringValues> allParamsDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uriQuery);   // unlike ParseQueryString in System.Web, this returns a dictionary of type IDictionary<string, string[]>, so the value is an array of strings. This is how the dictionary handles multiple query string parameters with the same name.
                StringValues queryStrVal;
                if (!allParamsDict.TryGetValue("yffOutFormat", out queryStrVal))
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: yffOutFormat= was not found. Uri: " + uriQuery + @""" }", "application/json");
                }
                string outputFormat = queryStrVal[0];
                bool isOutputJson = !String.Equals(outputFormat, "csv", StringComparison.CurrentCultureIgnoreCase);

                string yffColumns = null;
                List<string> yffColumnsList = new List<string>();
                if (allParamsDict.TryGetValue("yffColumns", out queryStrVal))
                {
                    yffColumns = queryStrVal[0];
                    yffColumnsList = new List<string>();
                    int columnsFormatStartIdx = 0;
                    string previousCommand = null;
                    for (int k = 1; k < yffColumns.Length; k++)
                    {
                        if (Char.IsLetter(yffColumns[k]))
                        {
                            // process previous command
                            previousCommand = yffColumns.Substring(columnsFormatStartIdx, k - columnsFormatStartIdx);
                            yffColumnsList.Add(previousCommand);
                            columnsFormatStartIdx = k;
                        }
                    }
                    previousCommand = yffColumns.Substring(columnsFormatStartIdx, yffColumns.Length - columnsFormatStartIdx);
                    yffColumnsList.Add(previousCommand);
                } else
                {
                    yffColumnsList = new List<string>() { "d", "o", "h", "l", "c", "c1", "v" };
                }


                string jsonpCallback = null;
                if (allParamsDict.TryGetValue("jsonp", out queryStrVal))
                {
                    jsonpCallback = queryStrVal[0];
                }
                string outputVariable = null;
                if (allParamsDict.TryGetValue("yffOutVar", out queryStrVal))
                {
                    outputVariable = queryStrVal[0];
                }
                if (jsonpCallback == null && outputVariable == null)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: nor yffOutVar= , neiher jsonp= was found. Uri: " + uriQuery + @""" }", "application/json");
                }
                else if (jsonpCallback != null && outputVariable != null)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: Both yffOutVar= ,  jsonp= were found. Uri: " + uriQuery + @""" }", "application/json");
                }

                string targetUriWithoutHttp = null;
                if (allParamsDict.TryGetValue("yffUri", out queryStrVal))
                {
                    targetUriWithoutHttp = queryStrVal[0];
                }

                // 2. Obtain Token.Cookie and Crumb (maybe from cash until 12 hours) that is needed for Y!F API from 2017-05
                // after 2017-05: https://query1.finance.yahoo.com/v7/finance/download/VXX?period1=1492941064&period2=1495533064&interval=1d&events=history&crumb=VBSMphmA5gp
                // but we will accept standard dates too and convert to Unix epoch before sending it to YF
                // https://query1.finance.yahoo.com/v7/finance/download/VXX?period1=2017-02-31&period2=2017-05-23&interval=1d&events=history&crumb=VBSMphmA5gp
                // https://github.com/dennislwy/YahooFinanceAPI
                int maxTryCrumb = 10;                 //first get a valid token from Yahoo Finance
                while (maxTryCrumb >= 0 && (string.IsNullOrEmpty(Token.Cookie) || string.IsNullOrEmpty(Token.Crumb)))
                {
                    Token.Refresh();
                    maxTryCrumb--;
                }

                // 3. With the Token.Cookie and Crumb download YF CSV file
                string startTimeStr = allParamsDict["period1"];
                string endTimeStr = allParamsDict["period2"];
                if (startTimeStr.IndexOf('-') != -1)    // format '2017-05-20' has hyphen in it; if it has hyphen, try to convert to Date.
                {
                    DateTime date = DateTime.ParseExact(startTimeStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    startTimeStr = Utils.DateTimeUtcToUnixTimeStamp(date).ToString();
                }
                if (String.Equals(endTimeStr, "UtcNow", StringComparison.CurrentCultureIgnoreCase))    // format '2017-05-20' has hyphen in it; if it has hyphen, try to convert to Date.
                {
                    endTimeStr = Utils.DateTimeUtcToUnixTimeStamp(DateTime.UtcNow).ToString();
                }
                else if (endTimeStr.IndexOf('-') != -1)    // format '2017-05-20' has hyphen in it; if it has hyphen, try to convert to Date.
                {
                    DateTime date = DateTime.ParseExact(endTimeStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (date.Date == DateTime.UtcNow.Date)  // if today is included, we thing the caller wants the real-time (latest) data; so change endTime to UtcNow
                        date = DateTime.UtcNow;
                    else
                        date = date.AddHours(12);    // endTimeStr Date is excluded from daily data, so Add a little bit more
                    endTimeStr = Utils.DateTimeUtcToUnixTimeStamp(date).ToString();
                }

                var csvDownload = string.Empty;
                string yfURI = String.Format("https://{0}?period1={1}&period2={2}&interval={3}&events={4}&crumb={5}", targetUriWithoutHttp, startTimeStr, endTimeStr, allParamsDict["interval"], allParamsDict["events"], Token.Crumb ?? "");
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(yfURI);
                request.CookieContainer = new CookieContainer();
                request.Headers[HttpRequestHeader.Cookie] = Token.Cookie;
                request.Method = "GET";
                var task = request.GetResponseAsync();
                task.Wait(10000); // Blocks current thread until GetFooAsync task completes; timeout is 10000msec
                using (var response = (HttpWebResponse)task.Result)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        csvDownload = new StreamReader(stream).ReadToEnd();
                    }
                }

                // 4.1 Process YF CSV file either as JSON or as CSV: Header
                StringBuilder responseStrBldr = new StringBuilder();
                bool wasDataLineWritten = false;        // real data line, not header line
                if (isOutputJson)
                {
                    if (outputVariable != null)
                    {
                        responseStrBldr.Append("var " + outputVariable + " = [\n");
                    }
                    if (jsonpCallback != null)
                    {
                        responseStrBldr.Append(jsonpCallback + "([\n");
                    }
                } else
                { 
                    WriteRow(isOutputJson, yffColumnsList, new string[] { "Date", "Open", "High", "Low", "Close", "Adj Close", "Volume" }, responseStrBldr, ref wasDataLineWritten);
                }

                // 4.2 Process YF CSV file either as JSON or as CSV: Data lines
                // First is the header, so skip it. Previously, it was upside down, so the latest was the first, but from 2017-05, the oldest is the first. Fine. Leave it like that.
                string[] lines = csvDownload.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] cells = lines[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cells.Length != 7)
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: yF row doesn't have 7 cells: " + lines[i] + @""" }", "application/json");
                    }

                    DateTime date;
                    if (!DateTime.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: problem with date format: " + cells[0] + @""" }", "application/json");
                    }
                    if (isOutputJson)
                        cells[0] = String.Format(@"Date.UTC({0},{1},{2})", date.Year, date.Month - 1, date.Day);
                    else
                        cells[0] = String.Format(@"{0}-{1}-{2}", date.Year, date.Month, date.Day);

                    // Prices in the given CSV as "15.830000" is pointless. Convert it to "15.8" if possible, 		"16.059999"	should be converted too
                    for (int j = 1; j < 6; j++)
                    {
                        if (Double.TryParse(cells[j], out double price))
                            cells[j] = price.ToString("0.###");
                    }

                    //Volume is sometimes "000"; convert it to "0"
                    if (Int64.TryParse(cells[6], out long volume))  // Volume was the 5th index, not it is the 6th index (the last item)
                        cells[6] = volume.ToString();

                    WriteRow(isOutputJson, yffColumnsList, cells, responseStrBldr, ref wasDataLineWritten);
                }

                // 4.3 Process YF CSV file either as JSON or as CSV: Footer and finalizing it
                if (outputVariable != null)
                    responseStrBldr.Append("];");
                if (jsonpCallback != null)
                    responseStrBldr.Append("]);");

                return new Tuple<string, string>(responseStrBldr.ToString(), "application/javascript");
            }
            catch (Exception e)
            {
                return new Tuple<string, string>(@"{ ""Message"":  ""Exception caught by WebApi Get(): " + e.Message + @""" }", "application/json");
            }
        }

        private readonly Dictionary<string, int> cCommandToIndDict = new Dictionary<string, int>() { { "d", 0 }, { "o", 1 }, { "h", 2 } , { "l", 3 } , { "c", 4 } , { "c1", 5 }, { "v", 6 } };

        private void WriteCell(string p_command, string[] cells, StringBuilder responseStrBldr, ref bool wasCellWritten)
        {
            if (wasCellWritten)
                responseStrBldr.AppendFormat(",");
            responseStrBldr.Append(cells[cCommandToIndDict[p_command]]);
            wasCellWritten = true;

        }

        private void WriteRow(bool p_isOutputJson, List<string> yffColumnsList, string[] cells, StringBuilder responseStrBldr, ref bool wasDataLineWritten)
        {
            if (wasDataLineWritten)
            {
                if (p_isOutputJson)
                    responseStrBldr.AppendFormat(",\n");
                else
                    responseStrBldr.AppendFormat("\n");
            }

            if (p_isOutputJson)
                responseStrBldr.AppendFormat("[");

            bool wasCellWritten = false;
            foreach (var command in yffColumnsList)
            {
                WriteCell(command, cells, responseStrBldr, ref wasCellWritten);
            }
            if (p_isOutputJson)
                responseStrBldr.AppendFormat("]");
            wasDataLineWritten = true;
        }

    }
}
