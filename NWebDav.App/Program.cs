using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using log4net.Config;
using NWebDav.Server;
using NWebDav.Server.Handlers;
using NWebDav.Server.Stores;

namespace NWebDav.App
{
    class Program
    {
        private class BasicAuthentication : IBasicAuthentication
        {
            public bool CheckCredentials(string name, string password)
            {
                return password == "test";
            }
        }

        static void Main(string[] args)
        {
            // Configure LOG4NET
            XmlConfigurator.Configure();

            // Create a 
            //var basicAuthentication = new BasicAuthentication();
            //var requestHandlerFactory = new BasicAuthenticationRequestHandlerFactory(basicAuthentication);

            // Create WebDAV server
            using (var webDavServer = new WebDavServer(new DiskStore(@"C:\Users\Ramon"), AuthenticationSchemes.Anonymous))
            {
                webDavServer.Start("http://localhost:11111/");

                while (true)
                    Console.ReadLine();
            }
        }
    }
}
