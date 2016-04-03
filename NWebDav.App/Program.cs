using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NWebDav.App.LogAdapters;
using NWebDav.Server;
using NWebDav.Server.Logging;
using NWebDav.Server.Platform.DotNet45;
using NWebDav.Server.Stores;

namespace NWebDav.App
{
    internal class Program
    {
        private class BasicAuthentication : IBasicAuthentication
        {
            public bool CheckCredentials(string name, string password)
            {
                return password == "test";
            }
        }

        private static async void DispatchHttpRequestsAsync(HttpListener httpListener, CancellationToken cancellationToken )
        {
            // Create a basic authenticator
            var basicAuthentication = new BasicAuthentication();

            // Create a request handler factory that uses basic authentication
            var requestHandlerFactory = new BasicAuthenticationRequestHandlerFactory(basicAuthentication);

            // Create WebDAV dispatcher
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var webDavDispatcher = new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);

            HttpListenerContext httpListenerContext;
            while (!cancellationToken.IsCancellationRequested && (httpListenerContext = await httpListener.GetContextAsync().ConfigureAwait(false)) != null)
            {
                var httpContext = new HttpContext(httpListenerContext);

                // The returned task is not awaited by design to make multiple
                // parallel requests possible. With 'await' only a single
                // operation can be executed at a time.
                webDavDispatcher.DispatchRequestAsync(httpContext);
            }
        }

        private static void Main(string[] args)
        {
            // Use the Log4NET adapter for logging
            //var adapter = new Log4NetAdapter();
            var adapter = new DebugOutputAdapter();
            adapter.LogLevels.Add(LogLevel.Debug);
            adapter.LogLevels.Add(LogLevel.Info);
            LoggerFactory.Factory = adapter;

            using (var httpListener = new HttpListener())
            {
                httpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                httpListener.Realm = "WebDAV server";
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
