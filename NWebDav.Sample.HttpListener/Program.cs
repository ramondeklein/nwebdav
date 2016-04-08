using System;
using System.Net;
using System.Threading;

using NWebDav.Server;
using NWebDav.Server.Handlers;
using NWebDav.Server.HttpListener;
using NWebDav.Server.Logging;
using NWebDav.Server.Stores;

using NWebDav.Sample.HttpListener.LogAdapters;

namespace NWebDav.Sample.HttpListener
{
    internal class Program
    {
        //private static bool CheckCredentials(HttpListenerBasicIdentity httpListenerBasicIdentity)
        //{
        //    return httpListenerBasicIdentity.Password == "test";
        //}

        private static async void DispatchHttpRequestsAsync(System.Net.HttpListener httpListener, CancellationToken cancellationToken )
        {
            // Create a request handler factory that uses basic authentication
            var requestHandlerFactory = new RequestHandlerFactory();

            // Create WebDAV dispatcher
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var webDavDispatcher = new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);

            HttpListenerContext httpListenerContext;
            while (!cancellationToken.IsCancellationRequested && (httpListenerContext = await httpListener.GetContextAsync().ConfigureAwait(false)) != null)
            {
                //var httpContext = new HttpBasicContext(httpListenerContext, checkIdentity: CheckCredentials);
                var httpContext = new HttpContext(httpListenerContext);

                // The returned task is not awaited by design to make multiple
                // parallel requests possible. With 'await' only a single
                // operation can be executed at a time.
                webDavDispatcher.DispatchRequestAsync(httpContext);
            }
        }

        private static void Main(string[] args)
        {
            // Configure LOG4NET
            log4net.Config.XmlConfigurator.Configure();

            // Use the Log4NET adapter for logging
            //var adapter = new Log4NetAdapter();
            var adapter = new DebugOutputAdapter();
            adapter.LogLevels.Add(LogLevel.Debug);
            adapter.LogLevels.Add(LogLevel.Info);
            LoggerFactory.Factory = adapter;

            using (var httpListener = new System.Net.HttpListener())
            {
                //httpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                //httpListener.Realm = "WebDAV server";
                httpListener.Prefixes.Add("http://localhost:11111/");

                // Start the HTTP listener
                httpListener.Start();

                // Start dispatching requests
                var cancellationTokenSource = new CancellationTokenSource();
                DispatchHttpRequestsAsync(httpListener, cancellationTokenSource.Token);

                // Wait until somebody presses return
                Console.WriteLine("WebDAV server running. Press <X> to quit.");
                while (Console.ReadLine() != "EXIT")
                    ;

                cancellationTokenSource.Cancel();
            }
        }
    }
}
