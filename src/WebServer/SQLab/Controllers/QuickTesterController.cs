using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SqCommon;
using SQLab.Controllers.QuickTester.Strategies;
using Microsoft.AspNetCore.Authorization;

// http://localhost:5000/qt?jsonp=JSON_CALLBACK&StartDate=&EndDate=&strategy=TotM&BullishTradingInstrument=Long%20SPY&DailyMarketDirectionMaskSummerTotM=DD00U00.U&DailyMarketDirectionMaskSummerTotMM=D0UU.0U&DailyMarketDirectionMaskWinterTotM=UUUD.UUU&DailyMarketDirectionMaskWinterTotMM=DDUU.UU00UU
namespace SQLab.Controllers
{
    [Route("~/qt", Name = "qt")]
    [Route("~/QuickTester", Name = "QuickTester")]
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
            //var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); 
            //if (authorizedEmailErrResponse != null) return authorizedEmailErrResponse;
            var urlPath = (HttpContext.Request.Path.HasValue) ? HttpContext.Request.Path.Value.ToLower() : String.Empty;
            if (urlPath == "/qt")
            {
            Tuple<string, string> contentAndType = GenerateQtResponse().Result;
            return Content(contentAndType.Item1, contentAndType.Item2);
            }
            else
                return View();
        }

        private async Task<Tuple<string, string>> GenerateQtResponse()
        {
            string jsonpCallback = null;
            string uriQuery = "";
            try
            {
                uriQuery = this.HttpContext.Request.QueryString.ToString();    // "?s=VXXB,SVXY,^vix&f=ab&o=csv" from the URL http://localhost:58213/api/rtp?s=VXXB,XIV,^vix&f=ab&o=csv

                if (uriQuery.Length > 8192)
                {//When you try to pass a string longer than 8192 charachters, a faultException will be thrown. There is a solution, but I don't want
                    throw new Exception("Error caught by WebApi Get():: uriQuery is longer than 8192: we don't process that. Uri: " + uriQuery);
                }

                uriQuery = uriQuery.Substring(1);   // remove '?'
                uriQuery = Uri.UnescapeDataString(uriQuery);    // change %20 to ' ', and %5E to '^'  , "^VIX" is encoded in the URI as "^%5EVIX"

                // QueryString paramsQs = new QueryString("?" + p_params);
                Dictionary<string, StringValues> allParamsDict = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uriQuery);   // unlike ParseQueryString in System.Web, this returns a dictionary of type IDictionary<string, string[]>, so the value is an array of strings. This is how the dictionary handles multiple query string parameters with the same name.
                StringValues jsonpStrVal;
                if (allParamsDict.TryGetValue("jsonp", out jsonpStrVal))    // Strategy.ts.StartBacktest() doesn't fill this up, so 'jsonp' is not expected in query
                    jsonpCallback = jsonpStrVal[0];

                string startDateStr = allParamsDict["StartDate"][0];     // if parameter is not present, then it is Unexpected, it will crash, and caller Catches it. Good.
                string endDateStr = allParamsDict["EndDate"][0];
                string strategyName = allParamsDict["strategy"][0];

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

                string jsonString = (await AdaptiveUberVxx.GenerateQuickTesterResponse(generalParams, strategyName, allParamsDict));
                if (jsonString == null)
                    jsonString = (await TotM.GenerateQuickTesterResponse(generalParams, strategyName, allParamsDict));
                if (jsonString == null)
                    jsonString = await VXX_SPY_Controversial.GenerateQuickTesterResponse(generalParams, strategyName, allParamsDict);
                if (jsonString == null)
                    jsonString = (await LEtf.GenerateQuickTesterResponse(generalParams, strategyName, allParamsDict));
                if (jsonString == null)
                    jsonString = (await TAA.GenerateQuickTesterResponse(generalParams, strategyName, allParamsDict));

                if (jsonString == null)
                    throw new Exception("Strategy was not found in the WebApi: " + strategyName);

                string reply = ResponseBuilder(jsonpCallback, jsonString);



                //var jsonDownload = string.Empty;
                ////string queryString = @"?s=VXXB,SVXY,UWM,TWM,^RUT&f=l"; // without JsonP, these tickers are streamed all the time
                //Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Sending '{this.HttpContext.Request.QueryString.ToString()}'");
                //string reply = VirtualBrokerMessage.Send(this.HttpContext.Request.QueryString.ToString(), VirtualBrokerMessageID.GetRealtimePrice).Result;
                //Utils.Logger.Info($"RealtimePrice.GenerateRtpResponse(). Received '{reply}'");
                return new Tuple<string, string>(reply, "application/json");
            }
            catch (Exception e)
            {
                string errMsg = $"Error. Exception in QuickTester.GenerateRtpResponse() UriQuery: '{uriQuery}'";
                Utils.Logger.Error(e, errMsg);
                string reply = ResponseBuilder(jsonpCallback, @"{ ""errorMessage"":  """ + errMsg + @", Exception.Message: " + e.Message + @""" }");
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
