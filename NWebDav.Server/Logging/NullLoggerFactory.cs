using System;

namespace NWebDav.Server.Logging
{
    public class NullLoggerFactory : ILoggerFactory
    {
        private class Logger : ILogger
        {
            public bool IsLogEnabled(LogLevel logLevel) => false;
            public void Log(LogLevel logLevel, string message, Exception exception = null)
            {
            }
        }

        private static readonly ILogger DefaultLogger = new Logger();

        public ILogger CreateLogger(Type type) => DefaultLogger;
    }
}
