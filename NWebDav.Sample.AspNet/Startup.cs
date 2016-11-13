using Microsoft.Owin;
using NWebDav.Server.Logging;
using Owin;

[assembly: OwinStartup(typeof(NWebDav.Sample.AspNet.Startup))]
namespace NWebDav.Sample.AspNet
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Use the Log4NET adapter for logging
            LoggerFactory.Factory = new DebugOutputAdapter();
        }
    }
}
