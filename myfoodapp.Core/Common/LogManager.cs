using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace myfoodapp.Core.Common
{
    public class LogManager
    {
        private static ILogger Logger;

        private static LogManager instance;

        public static LogManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogManager();
                }
                return instance;
            }
        }

        public void SetInstance(ILogger currentLogger)
        {
            Logger = currentLogger;
        }

        private LogManager()
        {
         
        }

        public void AppendLog(Log newLog)
        {
            try
            {
                switch (newLog.type)
                {
                    case LogType.Information:
                        Logger.LogInformation(newLog.description);
                        break;
                    case LogType.Warning:
                        Logger.LogWarning(newLog.description);
                        break;
                    case LogType.System:
                        Logger.LogTrace(newLog.description);
                        break;
                    case LogType.Error:
                        Logger.LogError(newLog.exception, newLog.description);
                        break;
                    default:
                        break;
                }              
            }
            catch (Exception ex)
            {
                Console.Write(ex.InnerException);
            }
        }

        // public async Task<List<string>> GetLogsAsync()
        // {          
        //     return null;
        // }

     
        // public async Task ClearLog()
        // {
            
        // }
    }
}
