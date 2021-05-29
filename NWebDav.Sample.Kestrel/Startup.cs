using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using NWebDav.Server;
using NWebDav.Server.Stores;
using NWebDav.Server.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace NWebDav.Sample.Kestrel
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO: Migrate this logging with NWebDav logging - still up to date?
            services.AddLogging((logging) =>
            {
                logging.AddDebug();
                logging.AddConsole();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // Create the request handler factory
            var requestHandlerFactory = new RequestHandlerFactory();

            // Create WebDAV dispatcher
            var homeFolder = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
            var webDavDispatcher = new WebDavDispatcher(new DiskStore(homeFolder), requestHandlerFactory);

            app.Run(async context =>
            {
                // Create the proper HTTP context
                var httpContext = new AspNetCoreContext(context);

                // Dispatch request
                await webDavDispatcher.DispatchRequestAsync(httpContext).ConfigureAwait(false);
            });
        }
    }
}