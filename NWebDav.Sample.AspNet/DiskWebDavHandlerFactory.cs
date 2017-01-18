using System;
using NWebDav.Server;
using NWebDav.Server.AspNet;
using NWebDav.Server.Logging;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.AspNet
{
    public class DiskWebDavHandlerFactory : WebDavHandlerFactory
    {
        static DiskWebDavHandlerFactory()
        {
            LoggerFactory.Factory = new DebugOutputAdapter();
        }

        public DiskWebDavHandlerFactory() : base(GetWebDavDispatcher())
        {
        }

        private static IWebDavDispatcher GetWebDavDispatcher()
        {
            // Create a request handler factory that uses basic authentication
            var requestHandlerFactory = new RequestHandlerFactory();

            // Create a WebDAV dispatcher for the home folder
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);
        }
    }
}