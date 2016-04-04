using System;
using System.Linq;

using NWebDav.Server.Logging;

using NWebDav.Sample.Kestrel.LogAdapters;

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

            var mergedArgs = new[] { "--server", "Microsoft.AspNet.Server.Kestrel" }.Concat(args).ToArray();
            Microsoft.AspNet.Hosting.Program.Main(mergedArgs);
        }
    }
}