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
        public BackgroundWorker(ILogger logger)
        {
            var log = LogManager.GetInstance;
            log.AppendLog(Log.CreateLog("Background Worker Engaged", LogType.Information));

            var sg = SigfoxInterfaceManager.GetInstance;
            sg.InitSensors();
            //sg.SendMessage("00730285AAAAAAAA02410914");

            var atls = AtlasSensorManager.GetInstance;
            atls.InitSensors(false);

            //var ph = atls.RecordPhMeasure(false);
            //Console.WriteLine(ph);
            //var water = atls.RecordSensorsMeasure(SensorTypeEnum.waterTemperature,false);
            //Console.WriteLine(water);

        }

    }

}