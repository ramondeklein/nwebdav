using System;
using System.Net;
using System.Threading;
using System.Xml;
using NWebDav.Server;
using NWebDav.Server.Http;
using NWebDav.Server.HttpListener;
using NWebDav.Server.Logging;
using NWebDav.Server.Stores;
using NWebDav.Sample.HttpListener.LogAdapters;
using Microsoft.Extensions.Configuration;

namespace NWebDav.Sample.HttpListener
{
    internal class Program
    {
        private static async void DispatchHttpRequestsAsync(IConfigurationSection webDavConfig, System.Net.HttpListener httpListener, CancellationToken cancellationToken)
        {
            // Create a request handler factory that uses basic authentication
            var requestHandlerFactory = new RequestHandlerFactory();

            // Create WebDAV dispatcher
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var webDavDispatcher = new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);

            // Determine the WebDAV username/password for authorization
            // (only when basic authentication is enabled)
            var webdavUsername = webDavConfig["UserName"] ?? "test";
            var webdavPassword = webDavConfig["Password"] ?? "test";

            HttpListenerContext httpListenerContext;
            while (!cancellationToken.IsCancellationRequested && (httpListenerContext = await httpListener.GetContextAsync().ConfigureAwait(false)) != null)
            {
                // Determine the proper HTTP context
                IHttpContext httpContext;
                if (httpListenerContext.Request.IsAuthenticated)
                    httpContext = new HttpBasicContext(httpListenerContext, checkIdentity: i => i.Name == webdavUsername && i.Password == webdavPassword);
                else
                    httpContext = new HttpContext(httpListenerContext);

                // Dispatch the request
                await webDavDispatcher.DispatchRequestAsync(httpContext).ConfigureAwait(false);
            }
        }

        private static void Main(string[] args)
        {
            // Use the Log4NET adapter for logging
            LoggerFactory.Factory = new ConsoleAdapter();

            var builder = new ConfigurationBuilder()
              .AddJsonFile(@"appsettings.json");
            var configuration = builder.Build();

            IConfigurationSection webDavSect = configuration.GetSection("WebDav");
            
            // Obtain the HTTP binding settings
            var webdavProtocol = webDavSect["Protocol"] ?? "http";
            var webdavIp = webDavSect["IP"] ?? "127.0.0.1";
            var webdavPort = webDavSect["Port"] ?? "11111";

            using (var httpListener = new System.Net.HttpListener())
            {
                // Add the prefix
                httpListener.Prefixes.Add($"{webdavProtocol}://{webdavIp}:{webdavPort}/");

                // Use basic authentication if requested
                var webdavUseAuthentication = XmlConvert.ToBoolean(webDavSect["webdav-authentication"] ?? "false");
                if (webdavUseAuthentication)
                {
                    // Check if HTTPS is enabled
                    if (webdavProtocol != "https")
                        Console.WriteLine("Most WebDAV clients cannot use authentication on a non-HTTPS connection");

                    // Set the authentication scheme and realm
                    httpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                    httpListener.Realm = "WebDAV server";
                }
                else
                {
                    // Allow anonymous access
                    httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                }

                // Start the HTTP listener
                httpListener.Start();

                // Start dispatching requests
                var cancellationTokenSource = new CancellationTokenSource();
                DispatchHttpRequestsAsync(webDavSect, httpListener, cancellationTokenSource.Token);

                // Wait until somebody presses return
                Console.WriteLine("WebDAV server running. Press 'x' to quit.");
                while (Console.ReadKey().KeyChar != 'x') ;

                cancellationTokenSource.Cancel();
            }
        }
    }
}
