using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SQLab.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet("[action]")]       // from the Route template "template: "{controller=Home}/{action=Index}/{id?}");" only action is used.
        public async Task Login(string returnUrl) // (string returnUrl = "/")
        {
            await HttpContext.ChallengeAsync("Google",
                new AuthenticationProperties()
                {
                    RedirectUri = returnUrl ?? "/"      // if http://localhost/api/account/login is called directly, there is no returnURL, which is null. However if we pass Null to GoogleAuth, it will come back to this "/login" which will cause an infinite loop. 
            });
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task Logout()
        {
            // TODO: redirect user to a nicer page that shows: ""You have been logged out. Goodbye " + context.User.Identity.Name + "<br>""
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            });
        }

        [Authorize]
        [HttpGet("[action]")]
        public IActionResult Profile()
        {
            return View();
        }
    }
}