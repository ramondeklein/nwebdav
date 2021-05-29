using Microsoft.AspNetCore.Hosting;

using NWebDav.Server.Logging;

using NWebDav.Sample.Kestrel.LogAdapters;
using Microsoft.Extensions.Hosting;

namespace NWebDav.Sample.Kestrel
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Use debug output for logging
            var adapter = new DebugOutputAdapter();
            adapter.LogLevels.Add(LogLevel.Debug);
            adapter.LogLevels.Add(LogLevel.Info);
            LoggerFactory.Factory = adapter;

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:11113");
                    webBuilder.UseStartup<Startup>();
                });

            host.Build().Run();

            //var host = new WebHostBuilder()
            //    .UseUrls("http://localhost:11113") // <----
            //    .UseKestrel(options => {
            //        //options.ThreadCount = 4;
            //        //options.UseConnectionLogging();
            //    })
            //    .UseStartup<Startup>()
            //    .Build();
            //host.Run();
        }
    }
}