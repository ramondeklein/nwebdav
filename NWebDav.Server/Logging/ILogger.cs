using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NWebDav.Server.Logging
{
    public interface ILogger
    {
        bool IsLogEnabled(LogLevel logLevel);
        void Log(LogLevel logLevel, string message, Exception exception = null);
    }
}
