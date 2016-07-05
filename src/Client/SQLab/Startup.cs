using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using SqCommon;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Text.Encodings.Web;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace SQLab
{
    public interface IWebAppGlobals
    {
        DateTime WebAppStartTime { get; set; }
    }

    class WebAppGlobals : IWebAppGlobals
    {
        DateTime m_webAppStartTime = DateTime.UtcNow;

        public DateTime WebAppStartTime
        {
            get
            {
                return m_webAppStartTime;
            }
            set
            {
                m_webAppStartTime = value;
            }
        }
    }

    public class Startup
    {
        //private static Microsoft.Extensions.Logging.ILogger mStartupLogger;     // not used in Controllers

        //private static SqCommon.IConfigurationRoot SqConfiguration { get; set; }   // don't maket it as a global variable, do DI, dependency injection to services

        private static IWebAppGlobals WebAppGlobals { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            //string configJsonPath = (Utils.RunningPlatform() == Platform.Linux) ?
            //        "/home/ubuntu/SQ/Client/SQLab/SQLab.Client.SQLab.NoGitHub.json" :
            //        @"g:\agy\Google Drive\GDriveHedgeQuant\shared\GitHubRepos\NonCommitedSensitiveData\SQLab.Client.SQLab.NoGitHub.json";
            //builder.AddJsonFile(configJsonPath, optional: true);       // somehow, it didn't work. But anyway, it is better to use SQCommon.Config, because that makes the x64 translation only once, at start, not every time

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();    // this is ASP.Net Config, not Utils.Configuration
        }

        public Microsoft.Extensions.Configuration.IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services. 
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
            services.AddSingleton(_ => Utils.Configuration);      // this is the proper DependenciInjection (DI) way of pushing it as a service to Controllers
            services.AddSingleton(_ => WebAppGlobals);
        }

        //public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        public static void ConfigureStaticFiles(IApplicationBuilder app, IHostingEnvironment hostEnv, ILoggerFactory loggerFactory)
        {
            // Configure the options to deploy
            var fileServerOptions = new FileServerOptions();
            // Add the physical path to the node_modules folder
            fileServerOptions.FileProvider = new PhysicalFileProvider(
                (Utils.RunningPlatform() == SqCommon.Platform.Linux) ?
                            "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/node_modules" :
                            @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\node_modules"
                //Path.Combine(appEnv.ApplicationBasePath, "node_modules")
            );

            // Add the request path
            // (http://docs.asp.net/en/latest/...
            fileServerOptions.RequestPath = new PathString("/node_modules");
            // With this setting on ASP will search for the following files upon startup
            // default.htm
            // default.html
            // index.htm
            // index.html
            fileServerOptions.EnableDefaultFiles = false;
            // If we are in development then allow directory browsing
            fileServerOptions.EnableDirectoryBrowsing = hostEnv.IsDevelopment();
            // Add those options to the file server
            app.UseFileServer(fileServerOptions);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostEnv, ILoggerFactory loggerFactory)
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
            loggerFactory.AddConsole(Configuration.GetSection("LoggingToConsole"));
            //loggerFactory.AddConsole(LogLevel.Debug);     // write to the Console  (if available) window as Colorful multiline (in Kestrel??) . MinLevel can be specified. by default it is LogLevel.Information
            loggerFactory.AddDebug(Microsoft.Extensions.Logging.LogLevel.Debug);       // write to the Debug output window (in VS). MinLevel can be specified. by default it is LogLevel.Information
            // set nLog here if NLog works properly
            loggerFactory.AddProvider(new SQLabAspLoggerProvider());

            // using https://github.com/aspnet/Security/blob/59fc691f4152e6d5017176c0b700ee9834640481/samples/SocialSample/Startup.cs
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                //AuthenticationScheme = "MyCookieMiddlewareInstance",
                CookieName = ".SQ.AspNetCore.Cookies", // ".AspNetCore.Cookies"
                AutomaticAuthenticate = true,   // default is true
                AutomaticChallenge = true,
                LoginPath = new PathString("/login"),    // if CloudFront string is in the Request Header, then the "/login" should be "https:www.snifferquant.net/login". However, this is a global setting, not URL request dependent. 
                //So, whatever, leave the temporary solution that Google authentication is HTTP, but after that we move back to HTTPS
                SlidingExpiration = true,   // default is true
                ExpireTimeSpan = TimeSpan.FromDays(30),  // default is 14 days
                                                         //CookieHttpOnly = false, // default: true.The default is true, which means the cookie will only be passed to only HTTP requests and is not made available to JS script on the page
                                                         //CookieSecure = CookieSecureOption.Never // default: CookieSecurePolicy.SameAsRequest;
            });

            if (!String.IsNullOrEmpty(Utils.Configuration["GoogleClientId"]) && !String.IsNullOrEmpty(Utils.Configuration["GoogleClientSecret"]))
            {
                //mStartupLogger.LogInformation("A_G_CId and A_G_CSe from Config has been found. Initializing GoogelAuthentication.");
                app.UseGoogleAuthentication(new GoogleOptions
                {
                    ClientId = Utils.Configuration["GoogleClientId"],
                    ClientSecret = Utils.Configuration["GoogleClientSecret"],

                    //     SaveTokens: Defines whether access and refresh tokens should be stored in the Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties
                    //     after a successful authorization. This property is set to false by default to
                    //     reduce the size of the final authentication cookie.
                    SaveTokens = true,                    
                    Events = new OAuthEvents()
                    {
                        OnRemoteFailure = ctx =>
                        {
                            ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                            ctx.HandleResponse();
                            return Task.FromResult(0);
                        }
                    }
                });
            }
            else
            {
                Console.WriteLine("A_G_CId and A_G_CSe from Config has NOT been found. Cannot initialize GoogelAuthentication.");
                Utils.Logger.Warn("A_G_CId and A_G_CSe from Config has NOT been found. Cannot initialize GoogelAuthentication.");
            }

            app.UseStaticFiles();   // without it, the Server will not return static files like favicon.ico or other htm files

            ConfigureStaticFiles(app, hostEnv, loggerFactory);

            // Choose an authentication type
            app.Map("/login", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    //string cloudFrontSuccessfulAuthUri = String.Empty; // "https"
                    //foreach (var header in context.Request.Headers)
                    //{
                    //    Console.WriteLine($"{header.Key} : {header.Value}");
                    //    if (header.Key == "CloudFront-Forwarded-Proto")
                    //    {
                    //        //cloudFrontForwardedProto = header.Value + "://"; //"https"
                    //        cloudFrontSuccessfulAuthUri = "https://" + context.Request.Host.ToString();          // if "CloudFront-Forwarded-Proto" has been found temporary overwrite to HTTPS, even if it comes from HTTP. So we redirect HTTP back to HTTPS
                    //        Console.WriteLine($"cloudFrontForwardedProto = '{header.Value}'");
                    //    }
                    //}



                    var authType = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        //To create a cookie holding your user information you must construct a ClaimsPrincipal holding the information you wish to be serialized in the cookie. Once you have a suitable ClaimsPrincipal inside your controller method call
                        //await HttpContext.Authentication.SignInAsync("MyCookieMiddlewareInstance", principal);
                        //await context.Authentication.SignInAsync(authType, )
                        //var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") }, CookieAuthenticationDefaults.AuthenticationScheme));
                        //await context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);

                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        await context.Authentication.ChallengeAsync(authType, new AuthenticationProperties() {

                            IsPersistent = true,    // default: false. whether the authentication session cookie is persisted across multiple Browser sessions/requests (when user closes and restart Chrome). It was fixed in AspDotNetCore RC3
                            RedirectUri = "/" });
                        return;
                    }

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("Choose an authentication scheme: <br>");
                    foreach (var type in context.Authentication.GetAuthenticationSchemes())
                    {
                        if (type.AuthenticationScheme != "Cookies")
                            await context.Response.WriteAsync("<a href=\"?authscheme=" + type.AuthenticationScheme + "\">" + (type.DisplayName ?? "(suppressed)") + "</a><br>");
                    }
                    await context.Response.WriteAsync("</body></html>");
                });
            });

         


        // Sign-out to remove the user cookie.
        app.Map("/logout", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("You have been logged out. Goodbye " + context.User.Identity.Name + "<br>");
                    await context.Response.WriteAsync("<a href=\"/\">Home</a>");
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            // Display the remote error
            app.Map("/error", errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<html><body>");
                    await context.Response.WriteAsync("An remote failure has occurred: " + context.Request.Query["FailureMessage"] + "<br>");
                    await context.Response.WriteAsync("<a href=\"/\">Home</a>");
                    await context.Response.WriteAsync("</body></html>");
                });
            });

            //app.Map("/", indexApp =>    // The path must not end with a '/'
   
            //app.UseMvc();     // see new changes in ASP.Net Core 1.0 routing: http://www.inversionofcontrol.co.uk/asp-net-core-1-0-routing-under-the-hood/
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
                    //template: "{controller=Home}/{action=Index}/{id?}");
            });

            //app.Run(async context =>
            //{
            //    //string cloudFrontSuccessfulAuthUri = String.Empty; // "https"
            //    //if (context.Request.Path.ToString() == "/") // temporary log to see that Origin Custom Headers: CloudFrontSQNet=True is arrived or not
            //    //{
            //    Console.WriteLine($"Cookies:");
            //    foreach (var cookie in context.Request.Cookies)
            //    {
            //        Console.WriteLine($"{cookie.Key} : {cookie.Value}");
            //    }

            //    //    foreach (var header in context.Request.Headers)
            //    //    {
            //    //        Console.WriteLine($"{header.Key} : {header.Value}");
            //    //        if (header.Key == "CloudFront-Forwarded-Proto")
            //    //        {
            //    //            //cloudFrontForwardedProto = header.Value + "://"; //"https"
            //    //            cloudFrontSuccessfulAuthUri = "https://" + context.Request.Host.ToString();          // if "CloudFront-Forwarded-Proto" has been found temporary overwrite to HTTPS, even if it comes from HTTP. So we redirect HTTP back to HTTPS
            //    //            Console.WriteLine($"cloudFrontForwardedProto = '{header.Value}'");
            //    //        }
            //    //    }
            //    // }

            //    bool isAuthNeeded = true;   // some files, like static files, etc. will not require authentication. Only main Html codes require that.

            //    // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
            //    var user = context.User;

            //    // This is what [Authorize] calls
            //    // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

            //    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
            //    // var user = await context.Authentication.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

            //    // Deny anonymous request beyond this point.
            //    if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
            //    {
            //        // This is what [Authorize] calls
            //        // The cookie middleware will intercept this 401 and redirect to /login
            //        //await context.Authentication.ChallengeAsync();

            //        await context.Authentication.ChallengeAsync(new AuthenticationProperties()
            //        {
            //            IsPersistent = true    // (no effect; because "/login" will authenticate; default: false. whether the authentication session cookie is persisted across multiple Browser sessions/requests.
            //                                   // RedirectUri = "/"
            //        });

            //        // authProp. RedirectUri = the URI After succesfull Auth, which is the '/'. It is not the Uri for the unsuccesfull Auth. That can be found in the "Cookie" auth middleware config 

            //        //await context.Authentication.ChallengeAsync(new AuthenticationProperties()
            //        //{
            //        //    RedirectUri = cloudFrontSuccessfulAuthUri + "/"
            //        //});

            //        // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
            //        // await context.Authentication.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

            //        return;
            //    }

            //    // Display user information
            //    context.Response.ContentType = "text/html";

            //    // Display user information
            //    context.Response.ContentType = "text/html";

            //    switch (context.Request.Path.ToString())
            //    {
            //        case "/UserInfo":
            //            await ResponseUserInfo(context);
            //            break;
            //        case "/DeveloperDashboard":
            //            await ResponseDeveloperDashboard(context);
            //            break;
            //        default:
            //            await ResponseIndexHtml(context);
            //            break;
            //    }
            //});


        }

        //private static async Task ResponseIndexHtml(HttpContext p_context)
        //{
        //    await ResponseDeveloperDashboard(p_context);
        //}

        //private static async Task ResponseUserInfo(HttpContext p_context)
        //{
        //    await p_context.Response.WriteAsync("<html><body>");
        //    await p_context.Response.WriteAsync("Hello " + (p_context.User.Identity.Name ?? "anonymous") + "<br>");
        //    await p_context.Response.WriteAsync("Request.Path '" + (p_context.Request.Path.ToString() ?? "Empty") + "'<br>");
        //    foreach (var claim in p_context.User.Claims)
        //    {
        //        await p_context.Response.WriteAsync(claim.Type + ": " + claim.Value + "<br>");
        //    }

        //    await p_context.Response.WriteAsync("Tokens:<br>");
        //    await p_context.Response.WriteAsync("Access Token: " + await p_context.Authentication.GetTokenAsync("access_token") + "<br>");
        //    await p_context.Response.WriteAsync("Refresh Token: " + await p_context.Authentication.GetTokenAsync("refresh_token") + "<br>");
        //    await p_context.Response.WriteAsync("Token Type: " + await p_context.Authentication.GetTokenAsync("token_type") + "<br>");
        //    await p_context.Response.WriteAsync("expires_at: " + await p_context.Authentication.GetTokenAsync("expires_at") + "<br>");
        //    await p_context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
        //    await p_context.Response.WriteAsync("</body></html>");
        //}

        //private static async Task ResponseDeveloperDashboard(HttpContext p_context)
        //{
        //    string fullPath = (Utils.RunningPlatform() == Platform.Linux) ?
        //            "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/DeveloperDashboard.html" :
        //            @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\DeveloperDashboard.html";

        //    string fileStr = System.IO.File.ReadAllText(fullPath);
        //    await p_context.Response.WriteAsync(fileStr);  
        //}
    }
}
