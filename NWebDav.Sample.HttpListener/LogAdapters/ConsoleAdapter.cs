using System;
using NWebDav.Server.Logging;

namespace NWebDav.Sample.HttpListener.LogAdapters
{
    public class ConsoleAdapter : ILoggerFactory
    {
        private class ConsoleLogger : ILogger
        {
            private readonly Type _type;

            public ConsoleLogger(Type type)
            {
                _type = type;
            }

            public bool IsLogEnabled(LogLevel logLevel) => true;

            public void Log(LogLevel logLevel, Func<string> messageFunc, Exception exception)
            {
                if (exception == null)
                    Console.WriteLine($"{_type.Name} - {logLevel} - {messageFunc()}");
                else
                    Console.WriteLine($"{_type.Name} - {logLevel} - {messageFunc()}: {exception.Message}");
            }
        }

        public ILogger CreateLogger(Type type) => new ConsoleLogger(type);
    }
}
