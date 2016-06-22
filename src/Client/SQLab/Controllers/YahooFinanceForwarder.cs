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

// https://www.snifferquant.net/YahooFinanceForwarder?yffOutFormat=json&yffOutVar=myData1&yffUri=ichart.finance.yahoo.com/table.csv&s=%5EVIX&a=00&b=1&c=2014&d=01&e=21&f=2014&g=d&ignore=.csv
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

        [Authorize]
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
                string uriQuery = Request.QueryString.ToString();    // "?s=VXX,XIV,^vix&f=ab&o=csv" from the URL http://localhost:58213/api/rtp?s=VXX,XIV,^vix&f=ab&o=csv
                if (uriQuery.Length > 8192)
                {//When you try to pass a string longer than 8192 charachters, a faultException will be thrown. There is a solution, but I don't want
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error caught by WebApi Get():: uriQuery is longer than 8192: we don't process that. Uri: " + uriQuery + @""" }", "application/json");
                }


                uriQuery = uriQuery.Substring(1);   // remove '?'
                if (!uriQuery.StartsWith("yffOutFormat=", StringComparison.CurrentCultureIgnoreCase))
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: yffOutFormat= was not found. Uri: " + uriQuery + @""" }", "application/json");
                }
                uriQuery = uriQuery.Substring("yffOutFormat=".Length);
                int ind = uriQuery.IndexOf('&');
                if (ind == -1)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: uriQuery.IndexOf('&') 1. Uri: " + uriQuery + @""" }","application/json");
                }
                string outputFormat = uriQuery.Substring(0, ind);   // hopefully json or csv
                uriQuery = uriQuery.Substring(ind + 1);


                string yffColumns = null;
                if (uriQuery.StartsWith("yffColumns=", StringComparison.CurrentCultureIgnoreCase))
                {
                    uriQuery = uriQuery.Substring("yffColumns=".Length);
                    ind = uriQuery.IndexOf('&');
                    if (ind == -1)
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: uriQuery.IndexOf('&') 2. Uri: " + uriQuery + @""" }", "application/json");
                    }
                    yffColumns = uriQuery.Substring(0, ind);
                    uriQuery = uriQuery.Substring(ind + 1);
                }


                string jsonpCallback = null, outputVariable = null;
                if (uriQuery.StartsWith("jsonp=", StringComparison.CurrentCultureIgnoreCase))
                {
                    uriQuery = uriQuery.Substring("jsonp=".Length);
                    ind = uriQuery.IndexOf('&');
                    if (ind == -1)
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: uriQuery.IndexOf('&') 2. Uri: " + uriQuery + @""" }", "application/json");
                    }
                    jsonpCallback = uriQuery.Substring(0, ind);
                    uriQuery = uriQuery.Substring(ind + 1);
                }

                if (uriQuery.StartsWith("yffOutVar=", StringComparison.CurrentCultureIgnoreCase))
                {
                    uriQuery = uriQuery.Substring("yffOutVar=".Length);
                    ind = uriQuery.IndexOf('&');
                    if (ind == -1)
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: uriQuery.IndexOf('&') 2. Uri: " + uriQuery + @""" }", "application/json");
                    }
                    outputVariable = uriQuery.Substring(0, ind);   // hopefully json or csv
                    uriQuery = uriQuery.Substring(ind + 1);
                }

                if (jsonpCallback == null && outputVariable == null)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: nor yffOutVar= , neiher jsonp= was found. Uri: " + uriQuery + @""" }", "application/json");
                }
                else if (jsonpCallback != null && outputVariable != null)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: Both yffOutVar= ,  jsonp= were found. Uri: " + uriQuery + @""" }", "application/json");
                }





                if (!uriQuery.StartsWith("yffUri=", StringComparison.CurrentCultureIgnoreCase))
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: yffUri= was not found. Uri: " + uriQuery + @""" }", "application/json");
                }
                uriQuery = uriQuery.Substring("yffUri=".Length);
                ind = uriQuery.IndexOf('&');
                if (ind == -1)
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: uriQuery.IndexOf('&') 3. Uri: " + uriQuery + @""" }", "application/json");
                }
                string targetUriWithoutHttp = uriQuery.Substring(0, ind);   // hopefully json or csv
                uriQuery = uriQuery.Substring(ind + 1);



                string yfURI = @"http://" + targetUriWithoutHttp + "?" + uriQuery;
                var csvDownload = string.Empty;
                if (!Utils.DownloadStringWithRetry(out csvDownload, yfURI, 5, TimeSpan.FromSeconds(5), false))
                {
                    return new Tuple<string, string>(@"{ ""Message"":  ""Error: yF download was not succesfull: " + yfURI + @""" }", "application/json");
                }

                StringBuilder responseStrBldr = new StringBuilder();
                if (outputVariable != null)
                    responseStrBldr.Append("var " + outputVariable + " = [\n");
                if (jsonpCallback != null)
                    responseStrBldr.Append(jsonpCallback + "([\n");

                bool wasRecordWritten = false;
                string[] lines = csvDownload.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // First is the header, so skip it.
                // it is upside down, but don't reverse the order: usually the client static javascript likes if the latest is at the top (that is the first)
                //for (int i = lines.Length - 1; i > 0; i--)
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] cells = lines[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    // use the first (date) and the last (adjustedclose) cell only

                    if (cells.Length != 7)
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: yF row doesn't have 7 cells: " + lines[i] + @""" }", "application/json");
                    }

                    DateTime date;
                    if (!DateTime.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    {
                        return new Tuple<string, string>(@"{ ""Message"":  ""Error: problem with date format: " + cells[0] + @""" }", "application/json");
                    }

                    if (wasRecordWritten)
                        responseStrBldr.AppendFormat(",\n");
                    // like here http://www.highcharts.com/samples/data/usdeur.js, 


                    //Volume is sometimes "000"; convert it to "0"
                    string volumeStr = "NaN";
                    long volume = 0;
                    if (Int64.TryParse(cells[5], out volume))
                        volumeStr = volume.ToString();

                    if (yffColumns == null)
                    {
                        responseStrBldr.AppendFormat(@"[Date.UTC({0},{1},{2}),{3},{4},{5},{6},{7},{8}]", date.Year, date.Month - 1, date.Day, cells[1], cells[2], cells[3], cells[4], volumeStr, cells[6]);  // JS fuck! : Month starts from 0, but days not
                        wasRecordWritten = true;
                    }
                    else
                    {
                        responseStrBldr.AppendFormat(@"[");
                        bool wasCellWritten = false;

                        int columnsFormatStartIdx = 0;
                        string previousCommand = null;
                        for (int k = 1; k < yffColumns.Length; k++)
                        {
                            if (Char.IsLetter(yffColumns[k]))
                            {
                                // process previous command
                                previousCommand = yffColumns.Substring(columnsFormatStartIdx, k - columnsFormatStartIdx);
                                ProcessColumnCommand(previousCommand, cells, date, volumeStr, responseStrBldr, ref wasCellWritten);
                                columnsFormatStartIdx = k;
                            }
                        }

                        previousCommand = yffColumns.Substring(columnsFormatStartIdx, yffColumns.Length - columnsFormatStartIdx);
                        ProcessColumnCommand(previousCommand, cells, date, volumeStr, responseStrBldr, ref wasCellWritten);

                        responseStrBldr.AppendFormat(@"]");

                        wasRecordWritten = true;
                    }


                    //responseStrBldr.AppendFormat(@"[Date.UTC({0},{1},{2}),{3}]", date.Year, date.Month -1, date.Day, cells[cells.Length -1]);  // JS fuck! : Month starts from 0, but days not
                    //wasRecordWritten = true;
                }

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

        private void ProcessColumnCommand(string command, string[] cells, DateTime date, string volumeStr, StringBuilder responseStrBldr, ref bool wasCellWritten)
        {
            if (wasCellWritten)
                responseStrBldr.AppendFormat(",");

            if (String.Equals(command, "d", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.AppendFormat(@"Date.UTC({0},{1},{2})", date.Year, date.Month - 1, date.Day);
                wasCellWritten = true;
            }

            if (String.Equals(command, "o", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(cells[1]);
                wasCellWritten = true;
            }

            if (String.Equals(command, "h", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(cells[2]);
                wasCellWritten = true;
            }

            if (String.Equals(command, "l", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(cells[3]);
                wasCellWritten = true;
            }

            if (String.Equals(command, "c", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(cells[4]);
                wasCellWritten = true;
            }

            if (String.Equals(command, "v", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(volumeStr);
                wasCellWritten = true;
            }

            if (String.Equals(command, "c1", StringComparison.CurrentCultureIgnoreCase))
            {
                responseStrBldr.Append(cells[6]);
                wasCellWritten = true;
            }
        }

    }
}
