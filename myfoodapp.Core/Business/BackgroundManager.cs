using System;
using System.Collections.Generic;
using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text;

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

            for(int i = 0; i < 2; i++)
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
            sg.SendMessage("00730285AAAAAAAA02410914", SigfoxVersion.v2);

            var atls = AtlasSensorManager.GetInstance;
            atls.InitSensors(false);

            //var ph = atls.RecordPhMeasure(false);
            //Console.WriteLine(ph);
            //var water = atls.RecordSensorsMeasure(SensorTypeEnum.waterTemperature,false);
            //Console.WriteLine(water);
            
            try 
            {
                var rslt3 = "sudo /opt/vc/bin/vcgencmd measure_temp".Bash();
                Console.WriteLine(rslt3.Substring(5,2));
                rslt3 = rslt3.Substring(5,2);
                
                StringBuilder rslt2 = new StringBuilder("EEEE");
                rslt2[2] = rslt3[0];
                rslt2[3] = rslt3[1];
                Console.WriteLine(rslt2.ToString());

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

            UserSettingsManager mod = UserSettingsManager.GetInstance;
            
            var tt = mod.GetUserSettings();
            Console.WriteLine(tt.hubMessageAPI);

            using (var client = new HttpClient())
                {
                    var request = new Message()
                    {
                        content = "XXXXXXXXXXXX",
                        device = "DDDDDD",
                        date = "01-01-2020",
                        data = "Wifi"
                    };

                    var taskWeb = Task.Run(async () =>
                    {
                        try
                        {
                            var response = await client.PostAsync("https://hub.myfood.eu/api/Messages/",
                            new StringContent(JsonSerializer.Serialize(request),
                            Encoding.UTF8, "application/json"));

                            Console.WriteLine(response.ReasonPhrase);
                            Console.WriteLine(response.Content);

                            if (response.IsSuccessStatusCode)
                            {
                                //lg.AppendLog(Log.CreateLog("Measures sent to Azure via Internet", LogType.Information));
                            }
                        }
                        catch (Exception ex)
                        {
                            //lg.AppendLog(Log.CreateErrorLog("Exception on Measures to Azure", ex));
                        }
                    });

                    taskWeb.Wait();           
                }

        }

    }

}