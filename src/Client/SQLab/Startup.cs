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

namespace SQLab
{
    public class Startup
    {
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
            Configuration = builder.Build();
        }

        public Microsoft.Extensions.Configuration.IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services. 
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
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
            loggerFactory.AddConsole(Configuration.GetSection("LoggingToConsole"));
            //loggerFactory.AddConsole(LogLevel.Debug);     // write to the Console  (if available) window as Colorful multiline (in Kestrel??) . MinLevel can be specified. by default it is LogLevel.Information
            loggerFactory.AddDebug(Microsoft.Extensions.Logging.LogLevel.Debug);       // write to the Debug output window (in VS). MinLevel can be specified. by default it is LogLevel.Information
            // set nLog here if NLog works properly

            // After Configuring logging, set-up other things
            Utils.Configuration = Utils.InitConfigurationAndInitUtils("g:/agy/Google Drive/GDriveHedgeQuant/shared/GitHubRepos/NonCommitedSensitiveData/SQLab.Client.SQLab.NoGitHub.json", "/home/ubuntu/SQ/Client/SQLab/SQLab.Client.SQLab.NoGitHub.json");
            //Utils.MainThreadIsExiting = new ManualResetEventSlim(false);
            //HealthMonitorMessage.InitGlobals(HealthMonitorMessage.HealthMonitorServerPublicIpForClients, HealthMonitorMessage.DefaultHealthMonitorServerPort);       // until HealthMonitor runs on the same Server, "localhost" is OK
            //StrongAssert.g_strongAssertEvent += StrongAssertMessageSendingEventHandler;
            //TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException; // Occurs when a faulted task's unobserved exception is about to trigger exception which, by default, would terminate the process.



            // using https://github.com/aspnet/Security/blob/59fc691f4152e6d5017176c0b700ee9834640481/samples/SocialSample/Startup.cs
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = new PathString("/login")
            });

            if (!String.IsNullOrEmpty(Utils.Configuration["GoogleClientId"]) && !String.IsNullOrEmpty(Utils.Configuration["GoogleClientSecret"]))
            {
                //mStartupLogger.LogInformation("A_G_CId and A_G_CSe from Config has been found. Initializing GoogelAuthentication.");
                app.UseGoogleAuthentication(new GoogleOptions
                {
                    ClientId = Utils.Configuration["GoogleClientId"],
                    ClientSecret = Utils.Configuration["GoogleClientSecret"],
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
               // mStartupLogger.LogWarning("A_G_CId and A_G_CSe from Config has NOT been found. Cannot initialize GoogelAuthentication.");
            }

            app.UseStaticFiles();   // without it, the Server will not return static files like favicon.ico or other htm files

           
            // Choose an authentication type
            app.Map("/login", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    var authType = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        await context.Authentication.ChallengeAsync(authType, new AuthenticationProperties() { RedirectUri = "/" });
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





            app.Run(async context =>
            {
                bool isAuthNeeded = true;   // some files, like static files, etc. will not require authentication. Only main Html codes require that.

                // CookieAuthenticationOptions.AutomaticAuthenticate = true (default) causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.Authentication.AuthenticateAsync(AuthenticationManager.AutomaticScheme);

                // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                // var user = await context.Authentication.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                // Deny anonymous request beyond this point.
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will intercept this 401 and redirect to /login
                    await context.Authentication.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                    // await context.Authentication.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                    return;
                }

                // Display user information
                context.Response.ContentType = "text/html";

                switch (context.Request.Path.ToString())
                {
                    case "/UserInfo":
                        await ResponseUserInfo(context);
                        break;
                    case "/DeveloperDashboard":
                        await ResponseDeveloperDashboard(context);
                        break;
                    default:
                        await ResponseIndexHtml(context);
                        break;
                }

                
            });

            app.UseMvc();     // see new changes in ASP.Net Core 1.0 routing: http://www.inversionofcontrol.co.uk/asp-net-core-1-0-routing-under-the-hood/

        }

        private static async Task ResponseIndexHtml(HttpContext p_context)
        {
            await ResponseDeveloperDashboard(p_context);
        }

        private static async Task ResponseUserInfo(HttpContext p_context)
        {
            await p_context.Response.WriteAsync("<html><body>");
            await p_context.Response.WriteAsync("Hello " + (p_context.User.Identity.Name ?? "anonymous") + "<br>");
            await p_context.Response.WriteAsync("Request.Path '" + (p_context.Request.Path.ToString() ?? "Empty") + "'<br>");
            foreach (var claim in p_context.User.Claims)
            {
                await p_context.Response.WriteAsync(claim.Type + ": " + claim.Value + "<br>");
            }

            await p_context.Response.WriteAsync("Tokens:<br>");
            await p_context.Response.WriteAsync("Access Token: " + await p_context.Authentication.GetTokenAsync("access_token") + "<br>");
            await p_context.Response.WriteAsync("Refresh Token: " + await p_context.Authentication.GetTokenAsync("refresh_token") + "<br>");
            await p_context.Response.WriteAsync("Token Type: " + await p_context.Authentication.GetTokenAsync("token_type") + "<br>");
            await p_context.Response.WriteAsync("expires_at: " + await p_context.Authentication.GetTokenAsync("expires_at") + "<br>");
            await p_context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
            await p_context.Response.WriteAsync("</body></html>");
        }

        private static async Task ResponseDeveloperDashboard(HttpContext p_context)
        {
            string fullPath = (Utils.RunningPlatform() == Platform.Linux) ?
                    "/home/ubuntu/SQ/Client/SQLab/src/Client/SQLab/noPublishTo_wwwroot/DeveloperDashboard.html" :
                    @"g:\work\Archi-data\GitHubRepos\SQLab\src\Client\SQLab\noPublishTo_wwwroot\DeveloperDashboard.html";

            string fileStr = System.IO.File.ReadAllText(fullPath);
            await p_context.Response.WriteAsync(fileStr);  
        }
    }
}
