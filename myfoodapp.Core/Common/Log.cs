using System;

namespace myfoodapp.Core.Common
{
    public enum LogType
    {
        Information,
        Warning,
        System,
        Error
    }

    public class Log
    {        
        public string description { get; set; }
        public Exception exception { get; set; }
        public LogType type { get; set; }

        public Log() { }

        public static Log CreateLog(string _description, LogType _logType)
        {
            return new Log()
            {
                description = _description,
                type = _logType
            };
        }

        public static Log CreateErrorLog(string _description, Exception _exception)
        {
            return new Log()
            {
                description = _description,
                exception = _exception,
                type = LogType.Error
            };
        }
    }
}
