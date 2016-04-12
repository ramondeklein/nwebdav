using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;

using NWebDav.Server;
using NWebDav.Server.Handlers;
using NWebDav.Server.Stores;
using NWebDav.Server.AspNetCore;

namespace NWebDav.Sample.Kestrel
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationEnvironment env)
        {
            // Set the Kestrel server configuration
            var ksi = app.ServerFeatures.Get<IKestrelServerInformation>();
            //ksi.ThreadCount = 4;
            ksi.NoDelay = true;

            // Add logging
            // TODO: Migrate this logging with NWebDav logging
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddConsole(LogLevel.Debug);

            // Create the request handler factory
            var requestHandlerFactory = new RequestHandlerFactory();

            // Create WebDAV dispatcher
            var homeFolder = Environment.GetEnvironmentVariable("HOME");
            var webDavDispatcher = new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);

            app.Run(async context =>
            {
                var httpContext = new AspNetCoreContext(context);

                // The returned task is not awaited by design to make multiple
                // parallel requests possible. With 'await' only a single
                // operation can be executed at a time.
                webDavDispatcher.DispatchRequestAsync(httpContext);
            });
        }
    }
}