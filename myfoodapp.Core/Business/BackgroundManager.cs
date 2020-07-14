using System;
using System.Collections.Generic;
using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace myfoodapp.Core.Business
{
    public class BackgroundManager
    {
        public BackgroundManager()
        {
            var log = LogManager.GetInstance;
            log.AppendLog(Log.CreateLog("Background Worker Engaged", LogType.Information));

            var to = HumidityTemperatureManager.GetInstance;
            to.Connect();

            decimal capt = 0;

            for(int i = 0; i < 10; i++)
            {
                Task.Run(async() => 
                    {  
                        await Task.Delay(1000);
                        capt = (decimal)to.Temperature;
                        Console.WriteLine(capt);
                        await Task.Delay(1000);
                        capt = (decimal)to.Humidity;
                        Console.WriteLine(capt);
                        //to.Dispose();
                }).Wait(); 
            }  

            var clock = ClockManager.GetInstance;

            Task.Run(async() => 
            {
            clock.InitClock();
            await Task.Delay(3000);
            clock.SetDate(DateTime.Now);
            }).Wait(); 

            DateTime date = DateTime.Now;

            for(int i = 0; i < 2; i++)
            {
                Task.Run(async() => 
                    {
                        clock.InitClock();
                        await Task.Delay(1000);
                        date = clock.ReadDate();
                        Console.WriteLine(date.ToLongDateString());
                        clock.Dispose();
                }).Wait(); 
            };
            
            string setDay = String.Format(@"date +%Y%m%d -s ""{0}""", date.ToShortDateString());

            string setHour = String.Format(@"date +%T -s ""{0}""", date.ToShortDateString());
            
            var tt1 = setDay.Bash();
            Console.WriteLine(tt1);

            var tt11 = setHour.Bash();
            Console.WriteLine(tt1);

            Console.WriteLine("date".Bash());

            var allValues = Enum.GetValues(typeof(SensorTypeEnum));     

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
                var tt3 = "sudo /opt/vc/bin/vcgencmd measure_temp".Bash();
                Console.WriteLine(tt1);

                var tt2 = "sudo iwlist wlan0 scanning | grep ESSID".Bash();
                Console.WriteLine(tt2);
            } catch (Exception e) 
            {
            Console.WriteLine("{0} Exception caught.", e);
            }

            Console.WriteLine("Test Database Entities");
            var db = DatabaseModel.GetInstance;

            db.AddMesure(DateTime.Now, 7, SensorTypeEnum.pH).Wait();
            db.AddMesure(DateTime.Now, 8, SensorTypeEnum.pH).Wait();
            db.AddMesure(DateTime.Now, 9, SensorTypeEnum.pH).Wait();

            var rslt = new List<Measure>();
            Task.Run(() => 
            {
                 rslt = db.GetLastDayMesures(SensorTypeEnum.pH).Result;           
            }).Wait(1000);
            
            Console.WriteLine(rslt.Count);

            UserSettingsModel mod = UserSettingsModel.GetInstance;
            
            var tt = mod.GetUserSettings();
            Console.WriteLine(tt.hubMessageAPI);

        }

    }

}