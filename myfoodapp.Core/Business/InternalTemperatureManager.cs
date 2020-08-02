using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using myfoodapp.Core.Model;
using myfoodapp.Core.Common;

namespace myfoodapp.Core.Business
{
    public class InternalTemperatureManager
    {
        private LogManager lg = LogManager.GetInstance;
        private static InternalTemperatureManager instance;

        public static InternalTemperatureManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InternalTemperatureManager();
                }
                return instance;
            }
        }

        private InternalTemperatureManager()
        {
        }

        public string GetInternalTemperatureSignature()
        {
            StringBuilder rslt = new StringBuilder("EEEE");

            var str = "sudo /opt/vc/bin/vcgencmd measure_temp".Bash();

            str = str.Substring(5,2);
                
            rslt[2] = str[0];
            rslt[3] = str[1];
            
            return rslt.ToString();
        }
    }
}
