using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SQLab.Controllers
{
    public class HealthMonitorController : Controller
    {
#if !DEBUG
        [Authorize]
#endif
        //[RequireHttps]   see comments in Program.cs. AWS CloudFront kills this feature. Temporary, until We can use our own HTTPS SSL certificate, we are switching off this feature.
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}