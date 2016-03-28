using System;

namespace NWebDav.Server.Logging
{
    public static class LoggerFactory
    {
        private static readonly NullLoggerFactory DefaultLoggerFactory = new NullLoggerFactory();

        public static ILoggerFactory Factory { get; set; }

        public static ILogger CreateLogger(Type type)
        {
            var factory = Factory ?? DefaultLoggerFactory;
            return factory.CreateLogger(type);
        }
    }
}
