using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqCommon;
using System.Text;
using SQLab.Controllers.QuickTester.Strategies;
using Microsoft.AspNetCore.Authorization;

// http://localhost:5000/qt?jsonp=JSON_CALLBACK&StartDate=&EndDate=&strategy=TotM&BullishTradingInstrument=Long%20SPY&DailyMarketDirectionMaskSummerTotM=DD00U00.U&DailyMarketDirectionMaskSummerTotMM=D0UU.0U&DailyMarketDirectionMaskWinterTotM=UUUD.UUU&DailyMarketDirectionMaskWinterTotMM=DDUU.UU00UU
// http://localhost:5000/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=SRS-URE&rebalanceFrequency=5d
namespace SQLab.Controllers
{
    [Route("~/qt", Name = "qt")]
    public class QuickTesterController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public QuickTesterController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
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
            Tuple<string, string> contentAndType = GenerateRtpResponse().Result;
            return Content(contentAndType.Item1, contentAndType.Item2);

        }

        private async Task<Tuple<string, string>> GenerateRtpResponse()
        {
            string jsonpCallback = null;
            try
            {
                string uriQuery = this.HttpContext.Request.QueryString.ToString();    // "?s=VXX,XIV,^vix&f=ab&o=csv" from the URL http://localhost:58213/api/rtp?s=VXX,XIV,^vix&f=ab&o=csv

                if (uriQuery.Length > 8192)
                {//When you try to pass a string longer than 8192 charachters, a faultException will be thrown. There is a solution, but I don't want
                    throw new Exception("Error caught by WebApi Get():: uriQuery is longer than 8192: we don't process that. Uri: " + uriQuery);
                }

                uriQuery = uriQuery.Substring(1);   // remove '?'
                uriQuery = uriQuery.Replace("%20", " ").Replace("%5E", "^");    // de-coding from URL to normal things

                int ind = -1;
                if (uriQuery.StartsWith("jsonp=", StringComparison.CurrentCultureIgnoreCase))
                {
                    uriQuery = uriQuery.Substring("jsonp=".Length);
                    ind = uriQuery.IndexOf('&');
                    if (ind == -1)
                    {
                        throw new Exception("Error: uriQuery.IndexOf('&') 2. Uri: " + uriQuery);
                    }
                    jsonpCallback = uriQuery.Substring(0, ind);
                    uriQuery = uriQuery.Substring(ind + 1);
                }

                if (!uriQuery.StartsWith("StartDate=", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception("Error: StartDate= was not found. Uri: " + uriQuery);
                }
                uriQuery = uriQuery.Substring("StartDate=".Length);
                ind = uriQuery.IndexOf('&');
                if (ind == -1)
                {
                    ind = uriQuery.Length;
                }
                string startDateStr = uriQuery.Substring(0, ind);
                if (ind < uriQuery.Length)  // if we are not at the end of the string
                    uriQuery = uriQuery.Substring(ind + 1);
                else
                    uriQuery = "";

                if (!uriQuery.StartsWith("EndDate=", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception("Error: EndDate= was not found. Uri: " + uriQuery);
                }
                uriQuery = uriQuery.Substring("EndDate=".Length);
                ind = uriQuery.IndexOf('&');
                if (ind == -1)
                {
                    ind = uriQuery.Length;
                }
                string endDateStr = uriQuery.Substring(0, ind);
                if (ind < uriQuery.Length)  // if we are not at the end of the string
                    uriQuery = uriQuery.Substring(ind + 1);
                else
                    uriQuery = "";


                if (!uriQuery.StartsWith("strategy=", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new Exception("Error: strategy= was not found. Uri: " + uriQuery);
                }
                uriQuery = uriQuery.Substring("strategy=".Length);
                ind = uriQuery.IndexOf('&');
                if (ind == -1)
                {
                    ind = uriQuery.Length;
                }
                string strategyName = uriQuery.Substring(0, ind);
                if (ind < uriQuery.Length)  // if we are not at the end of the string
                    uriQuery = uriQuery.Substring(ind + 1);
                else
                    uriQuery = "";


                string strategyParams = uriQuery;

                DateTime startDate = DateTime.MinValue;
                if (startDateStr.Length != 0)
                {
                    if (!DateTime.TryParse(startDateStr, out startDate))
                        throw new Exception("Error: startDateStr couldn't be converted: " + uriQuery);
                }
                DateTime endDate = DateTime.MaxValue;
                if (endDateStr.Length != 0)
                {
                    if (!DateTime.TryParse(endDateStr, out endDate))
                        throw new Exception("Error: endDateStr couldn't be converted: " + uriQuery);
                }

                GeneralStrategyParameters generalParams = new GeneralStrategyParameters() { startDateUtc = startDate, endDateUtc = endDate };

                string jsonString = (await AdaptiveUberVxx.GenerateQuickTesterResponse(generalParams, strategyName, strategyParams));
                if (jsonString == null)
                    jsonString = (await TotM.GenerateQuickTesterResponse(generalParams, strategyName, strategyParams));
                if (jsonString == null)
                    jsonString = await VXX_SPY_Controversial.GenerateQuickTesterResponse(generalParams, strategyName, strategyParams);
                if (jsonString == null)
                    jsonString = (await LEtfDistcrepancy.GenerateQuickTesterResponse(generalParams, strategyName, strategyParams));
                if (jsonString == null)
                    jsonString = (await TAA.GenerateQuickTesterResponse(generalParams, strategyName, strategyParams));

                if (jsonString == null)
                    throw new Exception("Strategy was not found in the WebApi: " + strategyName);

                string reply = ResponseBuilder(jsonpCallback, jsonString);



                //var jsonDownload = string.Empty;
                ////string queryString = @"?s=VXX,SVXY,UWM,TWM,^RUT&f=l"; // without JsonP, these tickers are streamed all the time
                //Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Sending '{this.HttpContext.Request.QueryString.ToString()}'");
                //string reply = VirtualBrokerMessage.Send(this.HttpContext.Request.QueryString.ToString(), VirtualBrokerMessageID.GetRealtimePrice).Result;
                //Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Received '{reply}'");
                return new Tuple<string, string>(reply, "application/json");
            }
            catch (Exception e)
            {
                string reply = ResponseBuilder(jsonpCallback, @"{ ""errorMessage"":  ""Exception caught by WebApi Get(): " + e.Message + @""" }");
                return new Tuple<string, string>(reply, "application/json");
            }
        }

        public static string ResponseBuilder(string p_jsonpCallback, string p_jsonResponse)
        {
            StringBuilder responseStrBldr = new StringBuilder();
            if (p_jsonpCallback != null)
                responseStrBldr.Append(p_jsonpCallback + "(\n");
            responseStrBldr.Append(p_jsonResponse);
            if (p_jsonpCallback != null)
                responseStrBldr.Append(");");

            return responseStrBldr.ToString();
        }
    }
}
