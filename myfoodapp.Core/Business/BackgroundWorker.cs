using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using myfoodapp.Core.Business;

namespace myfoodapp.Core.Business
{
    public class BackgroundWorker
    {
        public BackgroundWorker()
        {
            var log = LogManager.GetInstance;
            log.AppendLog(Log.CreateLog("Background Worker Engaged", LogType.Information));

            var sg = SigfoxInterfaceManager.GetInstance;
            sg.InitInterface();
            sg.SendMessage("00730285AAAAAAAA02410914");

            var atls = AtlasSensorManager.GetInstance;
            atls.InitSensors(false);

            //var ph = atls.RecordPhMeasure(false);
            //Console.WriteLine(ph);
            //var water = atls.RecordSensorsMeasure(SensorTypeEnum.waterTemperature,false);
            //Console.WriteLine(water);
            
                        try 
                        {
                            var tt = "sudo /opt/vc/bin/vcgencmd measure_temp".Bash();
                            Console.WriteLine(tt);

                            var tt2 = "sudo iwlist wlan0 scanning | grep ESSID".Bash();
                            Console.WriteLine(tt2);
                        } catch (Exception e) 
                        {
                        Console.WriteLine("{0} Exception caught.", e);
                        }

        }

    }

}