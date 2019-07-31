using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace SQLab.Controllers
{
    [Route("~/", Name = "default")]
    // [Route("~/login", Name = "login")]
    //[Route("~/access_denied", Name = "access_denied")]
    [Route("~/app/{*url}", Name = "app")]
    [Route("~/DeveloperDashboard", Name = "DeveloperDashboard")]
    [Route("~/UserDashboard", Name = "UserDashboard")]
    [Route("~/VolatilityIndicesInDifferentMonths", Name = "VolatilityIndicesInDifferentMonths")]
    [Route("~/VXXAdaptiveConnorLiveBacktest", Name = "VXXAdaptiveConnorLiveBacktest")]
    //[Route("~/HealthMonitor", Name = "HealthMonitor")]
    //[Route("~/QuickTester", Name = "QuickTester")]
    [Route("~/WithdrawalSimulator", Name = "WithdrawalSimulator")]
    [Route("~/WithdrawalSimulatorHelp", Name = "WithdrawalSimulatorHelp")]
    [Route("~/VixFuturesAnalyser", Name = "VixFuturesAnalyser")]
    [Route("~/ContangoVisualizer", Name = "ContangoVisualizer")]
    [Route("~/AdvancedUberTAA", Name = "AdvancedUberTAA")]
    [Route("~/SINAddiction", Name = "SINAddiction")]
    [Route("~/VolatilityDragVisualizer", Name = "VolatilityDragVisualizer")]
    [Route("~/VolatilityDragVisualizerHelp", Name = "VolatilityDragVisualizerHelp")]
    [Route("~/GameChangerBetaCalculator", Name = "GameChangerBetaCalculator")]
    [Route("~/SQStudies", Name = "SQStudies")]
#if !DEBUG
    [Authorize]     // we can live without it, because ControllerCommon.CheckAuthorizedGoogleEmail() will redirect to /login anyway, but it is quicker that this automatically redirects without clicking another URL link.
#endif
    public class RootHomepageRedirectController : Controller
    {
        private readonly ILogger<Program> m_logger;
        private readonly SqCommon.IConfigurationRoot m_config;

        public RootHomepageRedirectController(ILogger<Program> p_logger, SqCommon.IConfigurationRoot p_config)
        {
            m_logger = p_logger;
            m_config = p_config;
        }

        public ActionResult Index()
        {
#if !DEBUG
            var authorizedEmailErrResponse = ControllerCommon.CheckAuthorizedGoogleEmail(this, m_logger, m_config); 
            if (authorizedEmailErrResponse != null) 
                return authorizedEmailErrResponse;
#endif
            var urlPath = (HttpContext.Request.Path.HasValue) ? HttpContext.Request.Path.Value.ToLower() : String.Empty;
#if DEBUG   // for the Index page, give Dashboard according to DEBUG or RELEASE
            if (String.IsNullOrWhiteSpace(urlPath) || urlPath == "/")
                urlPath = "/developerdashboard";
#else
            if (String.IsNullOrWhiteSpace(urlPath) || urlPath == "/")
                urlPath = "/userdashboard";
#endif
            string wwwRootPath = Program.RunningEnvStr(RunningEnvStrType.DontPublishToPublicWwwroot);                
            string fileName = String.Empty;
            switch (urlPath)
            {
                case "/userdashboard":
                    fileName = "UserDashboard.html";
                    break;
                case "/developerdashboard":
                    fileName = "DeveloperDashboard.html";
                    break;
                case "/volatilityindicesindifferentmonths":
                    fileName = "VolatilityIndicesInDifferentMonths.html";
                    break;
                case "/vxxadaptiveconnorlivebacktest":
                    fileName = "VXXAdaptiveConnorLiveBacktest.html";
                    break;
                //case "/healthmonitor":
                //    fileName = "HealthMonitor.html";
                //    break;
                //case "/quicktester":
                //    fileName = "QuickTester.html";
                //    break;
                case "/withdrawalsimulator":
                    fileName = "WithdrawalSimulator.html";
                    break;
                case "/withdrawalsimulatorhelp":
                    fileName = "WithdrawalSimulatorHelp.html";
                    break;
                case "/vixfuturesanalyser":
                    fileName = "VixFuturesAnalyser.html";
                    break;
                case "/contangovisualizer":
                    fileName = "ContangoVisualizer.html";
                    break;
                case "/advancedubertaa":
                    fileName = "AdvancedUberTAA.html";
                    break;
                case "/sinaddiction":
                    fileName = "SINAddiction.html";
                    break;
                case "/volatilitydragvisualizer":
                    fileName = "VolatilityDragVisualizer.html";
                    break;
                case "/volatilitydragvisualizerhelp":
                    fileName = "VolatilityDragVisualizerHelp.html";
                    break;
                case "/gamechangerbetacalculator":
                    fileName = "GameChangerBetaCalculator.html";
                    break;
                case "/sqstudies":
                    fileName = "SQStudiesList.html";
                    break;
                default:
#if DEBUG   // for the Index page, give Dashboard according to DEBUG or RELEASE
                    if (urlPath.EndsWith(".ts"))    // urlPath	"/app/quicktester/app.component.ts"
                    {
                        wwwRootPath = Program.RunningEnvStr(RunningEnvStrType.SQLabFolder);
                        fileName = urlPath.Substring(1); // remove first char '/'
                    }
                    else
                    {
                        m_logger.LogWarning($"HttpRequest: '{urlPath}' is not served.");
                    }
#endif
      
                    // not recognized, but it is here, because of Prefix. like "GET http://localhost/app/HealthMonintor/systemjs.config.js  "
                    //fileName = "UserDashboard.html";
                    break;
            }

            if (!String.IsNullOrEmpty(fileName))
            {
                string fileStr = System.IO.File.ReadAllText(wwwRootPath + fileName);

                switch (urlPath)    // replace unknown@gmail.com with proper gmail user if it is a main page
                {
                    case "/developerdashboard":
                    case "/userdashboard":
                        //case "/healthmonitor":
                        //case "/quicktester":

                        var clientUserEmail = WsUtils.GetRequestUser(this.HttpContext);
                        if (clientUserEmail == null)
                            clientUserEmail = "UnknownUser@gmail.com";

                        fileStr = fileStr.Replace("unknown@gmail.com", clientUserEmail);
                        break;
                    default:
                        break;
                }

                return Content(fileStr, "text/html");
            }
            else
                return NotFound();
        }

        
    }
}
