using System;

namespace NWebDav.Server.Logging
{
    public interface ILogger
    {
        bool IsLogEnabled(LogLevel logLevel);
        void Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null);
    }
}
