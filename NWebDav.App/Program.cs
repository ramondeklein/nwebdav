using System;
using System.Net;
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

        private static void Main(string[] args)
        {
            // Use the Log4NET adapter for logging
            LoggerFactory.Factory = new Log4NetAdapter();

            // Create a 
            //var basicAuthentication = new BasicAuthentication();
            //var requestHandlerFactory = new BasicAuthenticationRequestHandlerFactory(basicAuthentication);

            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:11111/");

            // Create WebDAV server
            var webDavServer = new WebDavServer(new DiskStore(@"C:\Users\Ramon"), new HttpListenerAdapter(httpListener));

            // Start the HTTP listener
            httpListener.Start();

            // Start the WebDAV server
            webDavServer.Start();

            while (true)
                Console.ReadLine();
        }
    }
}
