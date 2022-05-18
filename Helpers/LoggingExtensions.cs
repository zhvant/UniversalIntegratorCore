using SBAST.UniversalIntegrator.Configs;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Helpers
{
    public static class LoggingExtensions
    {
        public static LogLevel ToLogLevel(this LogLevelEnum logLevelEnum)
        {
            switch(logLevelEnum)
            {
                case LogLevelEnum.Debug:
                    return LogLevel.Debug;
                case LogLevelEnum.Error:
                    return LogLevel.Error;
                case LogLevelEnum.Fatal:
                    return LogLevel.Fatal;
                case LogLevelEnum.Info:
                    return LogLevel.Info;
                case LogLevelEnum.Trace:
                    return LogLevel.Trace;
                case LogLevelEnum.Warn:
                    return LogLevel.Warn;
                default:
                    return LogLevel.Off;
            }
        }
    }
}
