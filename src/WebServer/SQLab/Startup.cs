using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqCommon;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace SQLab
{
  
    public class Startup
    {
        public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            //Transient objects are always different; a new instance is provided to every controller and every service.
            //Scoped objects are the same within a request, but different across different requests
            //Singleton objects are the same for every object and every request(regardless of whether an instance is provided in ConfigureServices)
            services.AddSingleton(_ => Utils.Configuration);      // this is the proper DependenciInjection (DI) way of pushing it as a service to Controllers. So you don't have to manage the creation or disposal of instances.
            services.AddSingleton(_ => Program.g_webAppGlobals);

            string googleClientId = Utils.Configuration["GoogleClientId"];
            string googleClientSecret = Utils.Configuration["GoogleClientSecret"];
            if (!String.IsNullOrEmpty(googleClientId) && !String.IsNullOrEmpty(googleClientSecret))
            {
                // The reason you have BOTH google and cookies Auth is because you're using google for identity information but using cookies for storage of the identity for only asking Google once.
                //So AddIdentity() is not required, but Cookies Yes.
                services.AddAuthentication(options =>
                {
                    // If you don't want the cookie to be automatically authenticated and assigned to HttpContext.User, 
                    // remove the CookieAuthenticationDefaults.AuthenticationScheme parameter passed to AddAuthentication.
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;  // For anything else (sign in, sign out, authenticate, forbid), use the cookies scheme
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;   // For challenges, use the google scheme. If not, "InvalidOperationException: No authenticationScheme was specified"

                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(o => {  // CookieAuth will be the default from the two, GoogleAuth is used only for Challenge
                    o.LoginPath = "/account/login";
                    o.LogoutPath = "/account/logout";

                    // 2020-05-30: WARN|Microsoft.AspNetCore.Authentication.Google.GoogleHandler: '.AspNetCore.Correlation.Google.bzb7A4oxoS_pz_xQk0N4WngqgL0nyLUiT0k5QSPsD_M' cookie not found.
                    // "Exception: Correlation failed.".
                    // Maybe because SameSite cookies policy changed.
                    // I suspect Bunny used an old Chrome or FFox or Edge.
                    // "AspNetCore as a rule does not implement browser sniffing for you because User-Agents values are highly unstable"
                    // However, if updating browser of the user to the latest Chrome doesn't solve it, we may implement these changes:
                    // https://github.com/dotnet/aspnetcore/issues/14996
                    // https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                    // "Cookies without SameSite header are treated as SameSite=Lax by default.
                    // SameSite=None must be used to allow cross-site cookie use.
                    // Cookies that assert SameSite=None must also be marked as Secure. (requires HTTPS)"
                    // 2020-01: 'Correlation failed.' is a Browser Cache problem. 2020-06-03: JMC could log in. Error email 'correlation failed' arrived. When I used F12 in Chrome, disabled cache; then login went OK.

                    // 2020-08: Chrome implements this default behavior as of version 84. (2020-08). Edge doesn't restrict that yet.
                    // without any intervention, http://localhost/login returns this to the browser: ""Set-Cookie: .AspNetCore.Correlation.Google._AcFoUd0-sbBMoGfefWKA2WlqpVJwD2bGYTYs6axoBU=N; expires=Fri, 14 Aug 2020 14:45:30 GMT; path=/signin-google; samesite=none; httponly"
                    // and Chrome throws an Error to JsConsole: "A cookie associated with a resource at http://localhost/ was set with `SameSite=None` but without `Secure`. It has been blocked"
                    // disable this feature by going to "chrome://flags" and disabling "Cookies without SameSite must be secure", but it is good for development only
                    // So, from now on, because we want to use Chrome84+, if we want login, we have to develop in HTTPS mode, not HTTP. We can completely forget HTTP. Just use HTTPS, even in DEV. 

                    // >GoogleAuth Login system uses cookie (.AspNetCore.Correlation.Google). From 2020-08, Chrome blocks a SameSite=None, which is not Secure. 
                    // But Secure means it is running on HTTPS. So, local development will also need to be done with HTTPS urls.
                    // >Specify SameSite=None and Secure if the cookie should be sent in cross-site requests. This enables third-party use.
                    // Specify SameSite=Strict or SameSite=Lax if the cookie should not be sent in cross-site requests. 
                    // But even in this case, if we use Both HTTP, HTTPS at development, Login problems arise on HTTP.
                    // >Chrome debug: cookie HTTP://".AspNetCore.Cookies": "This set-cookie was blocked because it was not sent over a secure connection and would have overwritten a cookie with a secure attribute.", 
                    // but then that Secure HTTPS cookie with the same name is not sent to the non-secure HTTP request. (It is only sent to the HTTPS request).
                    // Therefore, we should use only the HTTPS protocol, even in local development.  (except if AWS CloudFront cannot handle HTTPS to HTTPS conversions)

                    // https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                    o.Cookie.SameSite = SameSiteMode.Lax;   // sets the cookie ".AspNetCore.Cookies"
                    // o.Cookie.SecurePolicy = CookieSecurePolicy.Always;      // Note this will also require you to be running on HTTPS. Local development will also need to be done with HTTPS urls.
                    // o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // this is the default BTW, so no need to set.
                    // problem: if Cookie storage works in https://localhost:5001/UserAccount/login  but not in HTTP: http://localhost:5000/UserAccount/login
                    // "Note that the http page cannot set an insecure cookie of the same name as the secure cookie."
                    // Solution: Manually delete the cookie from Chrome. see here.  https://bugs.chromium.org/p/chromium/issues/detail?id=843371
                    // in Production, only HTTPS is allowed anyway, so it will work. Best is not mix development in both HTTP/HTTPS (just stick to one of them). 
                    // stick to HTTPS. Although Chrome browser-caching will not work in HTTPS (because of insecure cert), it is better to test HTTPS, because that will be the production.


                    // Controls how much time the authentication ticket stored in the cookie will remain valid
                    // This is separate from the value of Microsoft.AspNetCore.Http.CookieOptions.Expires, which specifies how long the browser will keep the cookie. We will set that in OnTicketReceived()
                    o.ExpireTimeSpan = TimeSpan.FromDays(350);
                })
                .AddGoogle("Google", options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax; // sets the cookie ".AspNetCore.Correlation.Google.*"

                    options.Events = new OAuthEvents
                    {
                        // https://www.jerriepelser.com/blog/forcing-users-sign-in-gsuite-domain-account/
                        OnRedirectToAuthorizationEndpoint = context =>
                        {
                            Utils.Logger.Info("GoogleAuth.OnRedirectToAuthorizationEndpoint()");
                            //context.Response.Redirect(context.RedirectUri + "&hd=" + System.Net.WebUtility.UrlEncode("jerriepelser.com"));
                            context.Response.Redirect(context.RedirectUri);
                            return Task.CompletedTask;
                        },
                        OnCreatingTicket = context =>
                        {
                            string email = String.Empty;
                            var emailRec = context.User.Value<Newtonsoft.Json.Linq.JArray>("emails");
                            if (emailRec != null)
                                email = emailRec[0]["value"].ToString();
                            else
                                email = context.User.Value<string>("email");

                            Utils.Logger.Debug($"GoogleAuth.OnCreatingTicket(), [Authorize] attribute forced Google auth. Email:'{email ?? "null"}', RedirectUri: '{context.Properties.RedirectUri ?? "null"}'");

                            if (!Utils.IsAuthorizedGoogleUsers(Utils.Configuration, email))
                                throw new Exception($"Google Authorization Is Required. Your Google account: '{ email }' is not accepted. Logout this Google user and login with another one.");

                            //string domain = context.User.Value<string>("domain");
                            //if (domain != "jerriepelser.com")
                            //    throw new GoogleAuthenticationException("You must sign in with a jerriepelser.com email address");

                            return Task.CompletedTask;
                        },
                        OnTicketReceived = context =>
                        {
                            Utils.Logger.Info("GoogleAuth.OnTicketReceived()");
                            // if this is not set, then the cookie in the browser expires, even though the validation-info in the cookie is still valid. By default, cookies expire: "When the browsing session ends" Expires: 'session'
                            // https://www.jerriepelser.com/blog/managing-session-lifetime-aspnet-core-oauth-providers/
                            context.Properties.IsPersistent = true;
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(25);

                            return Task.FromResult(0);
                        },
                        OnRemoteFailure = context =>
                        {
                            Utils.Logger.Info("GoogleAuth.OnRemoteFailure()");
                            return Task.FromResult(0);
                        }
                    };
                });
            }
            else
            {
                Console.WriteLine("A_G_CId and A_G_CSe from Config has NOT been found. Cannot initialize GoogelAuthentication.");
                Utils.Logger.Warn("A_G_CId and A_G_CSe from Config has NOT been found. Cannot initialize GoogelAuthentication.");
            }

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(Configuration.GetSection("LoggingToConsole"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();

                // set nLog here if NLog works properly
                loggingBuilder.AddProvider(new SQLabAspLoggerProvider());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.MinimumLevel = LogLevel.Information;
            // root min level: you have to set this the most detailed, because if not ASPlog will not pass it to the NLogExtension
            //loggerFactory.MinimumLevel = Microsoft.Extensions.Logging.LogLevel.Debug;

            // A. ASP.NET5 LogLevels can come from appsettings.json or set here programmatically. But Configuration.GetSection("") returns text from the file, 
            // and not the file itself, so ConsoleLogger is not able to reload config when appsettings.json file is changed. 
            // For that you need a FileSystem watcher manually, which is not OK on Linux systems I guess or not on DNXCore
            // Therefore logging level cannot be changed by modifying that file (at least, not without extra FileSystemWatcher programming)
            // B. On the other hand Nlog is based on DNX, not DNXCore, and implements FileSystemWatcher properly, and I tested it and 
            // when the app.nlog file is changed by Notepad nLog under Asp.NET notices the LogLevelChange.
            
            //var x = Configuration.GetSection("LoggingToConsole");   // it is null
            //loggerFactory.AddConsole(Configuration.GetSection("LoggingToConsole"));
            ////loggerFactory.AddConsole(LogLevel.Debug);     // write to the Console  (if available) window as Colorful multiline (in Kestrel??) . MinLevel can be specified. by default it is LogLevel.Information
            //loggerFactory.AddDebug(Microsoft.Extensions.Logging.LogLevel.Trace);       // write to the Debug output window (in VS). MinLevel can be specified. by default it is LogLevel.Information
                        
            // set nLog here if NLog works properly
            // loggerFactory.AddProvider(new SQLabAspLoggerProvider());
            
            string envLogMsg = $"ASP env.EnvironmentName(machine-wide ASPNETCORE_ENVIRONMENT EnvVar or C# .UseEnvironment()):'{env.EnvironmentName}'";
            Console.WriteLine(envLogMsg);
            Utils.Logger.Info(envLogMsg);

            var aspLogLevel = Configuration.GetSection("Logging:LogLevel:Microsoft");
            string aspLogLevelStr = (aspLogLevel != null) ? aspLogLevel.Value : "NotAvailable";
            string logLevelMsg = $"ASP logLevel as appsettings.json or appsettings.Development.json:'Logging:LogLevel:Microsoft':'{aspLogLevelStr}'";
            Console.WriteLine(logLevelMsg);
            Utils.Logger.Info(logLevelMsg);

            if (env.IsDevelopment())
            {
                //Now, assuming you're running in development mode, any requests for files under /dist will be intercepted and served using Webpack dev middleware.
                //This is for development time only, not for production use (hence the env.IsDevelopment() check in the code above). 
                app.UseDeveloperExceptionPage();     // ExceptionHandlers will swallow the Exceptions. It will not be rolled further.
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true  //This watches for any changes you make to source files on disk (e.g., .ts/.html/.sass/etc. files), and automatically rebuilds them and pushes the result into your browser window, without even needing to reload the page.
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");     // ExceptionHandlers will swallow the Exceptions. It will not be rolled further.
            }

            app.UseMiddleware<SqFirewallMiddleware>();  // For this to catch Exceptions, it should come after UseExceptionHadlers(), because those will swallow exceptions and generates nice ErrPage.

            app.UseStaticFiles();   // Call UseWebpackDevMiddleware before UseStaticFiles 

            app.UseAuthentication();    // StaticFiles are served Before the user is authenticed. This is fast, but httpContext?.User?.Claims is null in this case.

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                // Since UseStaticFiles goes first, any requests that actually match physical files under wwwroot will be handled by serving that static file.
                //Since the default server - side MVC route goes next, any requests that match existing controller / action pairs will be handled by invoking that action.
                //Then, since MapSpaFallbackRoute is last, any other requests that don't appear to be for static files will be served by invoking the Index action on HomeController. 
                //This action's view should serve your client-side application code, allowing the client-side routing system to handle whatever URL has been requested.
                //Any requests that do appear to be for static files (i.e., those that end with filename extensions), will not be handled by MapSpaFallbackRoute, and so will end up as 404s.
                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });

        }
    }



}
