using myfoodapp.Core.Common;
using myfoodapp.Core.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace myfoodapp.Core.Business
{
    public sealed class MeasureBackgroundTask
    {
        private BackgroundWorker bw = new BackgroundWorker();
        private static readonly AsyncLock asyncLock = new AsyncLock();
        private AtlasSensorManager sensorManager;
        private HumidityTemperatureManager humTempManager;
        private SigfoxInterfaceManager sigfoxManager;
        private InternalTemperatureManager internalTemperatureManager;
        private UserSettings userSettings;
        private UserSettingsManager userSettingsManager = UserSettingsManager.GetInstance;
        private LogManager lg = LogManager.GetInstance;
        private DatabaseModel databaseModel = DatabaseModel.GetInstance;
        private int TICKSPERCYCLE = 600000;
        private int TICKSPERCYCLE_DIAGNOSTIC_MODE = 60000;
        public event EventHandler Completed;
        private static MeasureBackgroundTask instance;

        public static MeasureBackgroundTask GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MeasureBackgroundTask();
                }
                return instance;
            }
        }
        private MeasureBackgroundTask()
        {
            lg.AppendLog(Log.CreateLog("Measure Service starting...", LogType.System));

            userSettings = new UserSettings();

            userSettings = userSettingsManager.GetUserSettings();

            lg.AppendLog(Log.CreateLog("UserSettings retreived", LogType.System));

            //Disable Diagnostic Mode on Restart
            if(userSettings.isDiagnosticModeEnable)
            {
                userSettings.isDiagnosticModeEnable = false;
                userSettingsManager.SyncUserSettings(userSettings);
            }

            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = false;
            bw.DoWork += Bw_DoWork;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
        }

        public void Run()
        {
            if (bw.IsBusy)
                return;

            lg.AppendLog(Log.CreateLog("Measure Service running...", LogType.System));
            bw.RunWorkerAsync();
        }
        public void Stop()
        {
            bw.CancelAsync();
        }
        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lg.AppendLog(Log.CreateLog("Measure Service stopping...", LogType.System));
            Completed?.Invoke(this, EventArgs.Empty);
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var watch = Stopwatch.StartNew();

            var messageSignature = new StringBuilder("AAAAAAAAAAAAAAAAAAAAAAAA", 24);

            if (userSettings.measureFrequency >= 60000)
                TICKSPERCYCLE = userSettings.measureFrequency;

            var clockManager = ClockManager.GetInstance;

            var captureDateTime = DateTime.Now;

            if (clockManager != null)
            {
                Task.Run(async() => 
                {
                    clockManager.InitClock();
                    await Task.Delay(2000);
                    captureDateTime = clockManager.ReadDate();
                    clockManager.Dispose();
                }).Wait();               
            }

            userSettings = userSettingsManager.GetUserSettings();

            sigfoxManager = SigfoxInterfaceManager.GetInstance;

            if(userSettings.connectivityType == ConnectivityType.Sigfox)
            {
                sigfoxManager.InitInterface();
                sigfoxManager.SendMessage(messageSignature.ToString(), userSettings.sigfoxVersion);
            }

            if (userSettings.connectivityType == ConnectivityType.Wifi && NetworkInterface.GetIsNetworkAvailable())
            {
                Task.Run(async () =>
                {
                    await HttpClientHelper.SendMessage(userSettings.hubMessageAPI, 
                                                        messageSignature.ToString(), 
                                                        userSettings.productionSiteId);
                }).Wait();                                       
            }  

            if (userSettings.isDiagnosticModeEnable)
            {
                TICKSPERCYCLE = TICKSPERCYCLE_DIAGNOSTIC_MODE;
            }           
                
            sensorManager = AtlasSensorManager.GetInstance;

            sensorManager.InitSensors(userSettings.isSleepModeEnable);

            sensorManager.SetDebugLedMode(userSettings.isDebugLedEnable);

            humTempManager = HumidityTemperatureManager.GetInstance;

            internalTemperatureManager = InternalTemperatureManager.GetInstance;
            

            if (userSettings.isTempHumiditySensorEnable)
            {
                humTempManager.Connect();
            }

            while (!bw.CancellationPending)
            {
                var elapsedMs = watch.ElapsedMilliseconds;

                try
                {
                    if (elapsedMs % TICKSPERCYCLE == 0)
                    {
                            captureDateTime = captureDateTime.AddMilliseconds(TICKSPERCYCLE);

                            TimeSpan t = TimeSpan.FromMilliseconds(elapsedMs);

                            string logDescription = string.Format("[ {0:d} - {0:t} ] Service running since {1:D2}d:{2:D2}h:{3:D2}m:{4:D2}s",
                                                    captureDateTime,
                                                    t.Days,
                                                    t.Hours,
                                                    t.Minutes,
                                                    t.Seconds,
                                                    t.Milliseconds);

                            lg.AppendLog(Log.CreateLog(logDescription, LogType.Information));

                            var watchMesures = Stopwatch.StartNew();     

                            if (sensorManager.isSensorOnline(SensorTypeEnum.waterTemperature))
                            {
                                if (userSettings.isDiagnosticModeEnable)
                                    lg.AppendLog(Log.CreateLog("Water Temperature capturing", LogType.Information));

                                decimal capturedValue = 0;
                                capturedValue = sensorManager.RecordSensorsMeasure(SensorTypeEnum.waterTemperature, userSettings.isSleepModeEnable);

                                if (capturedValue > 0 && capturedValue < 80)
                                {
                                    messageSignature[4] = '0';
                                    messageSignature[5] = capturedValue.ToString()[0];
                                    messageSignature[6] = capturedValue.ToString()[1];
                                    messageSignature[7] = capturedValue.ToString()[3];

                                    if (!userSettings.isDiagnosticModeEnable)
                                    sensorManager.SetWaterTemperatureForPHSensor(capturedValue);

                                        var task = Task.Run(async () =>
                                        {
                                            await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.waterTemperature);
                                        });
                                        task.Wait();
                                
                                        if (userSettings.isDiagnosticModeEnable)
                                        {
                                            lg.AppendLog(Log.CreateLog(String.Format("Water Temperature captured : {0}", capturedValue), LogType.Information));
                                            var status = sensorManager.GetSensorStatus(SensorTypeEnum.waterTemperature, userSettings.isSleepModeEnable);
                                            lg.AppendLog(Log.CreateLog(String.Format("Water Temperature status : {0}", status), LogType.System));
                                        }     
                                }
                                else
                                {
                                    lg.AppendLog(Log.CreateLog(String.Format("Water Temperature value out of range - {0}", capturedValue), LogType.Warning));
                                    messageSignature[4] = 'B';
                                    messageSignature[5] = 'B';
                                    messageSignature[6] = 'B';
                                    messageSignature[7] = 'B';
                                }
                           }

                            if (sensorManager.isSensorOnline(SensorTypeEnum.pH))
                            {
                                if (userSettings.isDiagnosticModeEnable)
                                    lg.AppendLog(Log.CreateLog("PH capturing", LogType.Information));

                                decimal capturedValue = 0;
                                capturedValue = sensorManager.RecordpHMeasure(userSettings.isSleepModeEnable);

                                if (capturedValue > 1 && capturedValue < 12)
                                {
                                    messageSignature[0] = '0';
                                    messageSignature[1] = '0';
                                    messageSignature[2] = capturedValue.ToString()[0];
                                    messageSignature[3] = capturedValue.ToString()[2];

                                    var task = Task.Run(async () =>
                                    {
                                        await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.pH);
                                    });
                                    task.Wait();

                                    if (userSettings.isDiagnosticModeEnable)
                                    {
                                        lg.AppendLog(Log.CreateLog(String.Format("PH captured : {0}", capturedValue), LogType.Information));
                                        var status = sensorManager.GetSensorStatus(SensorTypeEnum.pH, userSettings.isSleepModeEnable);
                                        lg.AppendLog(Log.CreateLog(String.Format("PH status : {0}", status), LogType.System));
                                    }              
                                }
                                else
                                {
                                    lg.AppendLog(Log.CreateLog(String.Format("PH value out of range - {0}", capturedValue), LogType.Warning));
                                    messageSignature[0] = 'B';
                                    messageSignature[1] = 'B';
                                    messageSignature[2] = 'B';
                                    messageSignature[3] = 'B';
                                }
                            }
                            
                        if (userSettings.isTempHumiditySensorEnable)
                            {
                                try
                                {
                                    if (userSettings.isDiagnosticModeEnable)
                                        lg.AppendLog(Log.CreateLog("Air Temperature Humidity capturing", LogType.Information));

                                        decimal capturedValue = 0;

                                         Task.Run(async() => 
                                         {
                                            using (await asyncLock.LockAsync())
                                            {
                                             await Task.Delay(1000);
                                             capturedValue = (decimal)humTempManager.Temperature;
                                             Console.WriteLine("Temp" + capturedValue);
                                             await Task.Delay(1000);
                                             await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.airTemperature);
                                            
                                             messageSignature[16] = '0';
                                             messageSignature[17] = capturedValue.ToString()[0];
                                             messageSignature[18] = capturedValue.ToString()[1];
                                             messageSignature[19] = capturedValue.ToString()[3];

                                             if (userSettings.isDiagnosticModeEnable)
                                                 lg.AppendLog(Log.CreateLog(String.Format("Air Temperature captured : {0}", capturedValue), LogType.Information));
                                             
                                             await Task.Delay(1000);
                                             capturedValue = (decimal)humTempManager.Humidity;
                                             Console.WriteLine("Hum" + capturedValue);
                                             await Task.Delay(1000);    
                                             await databaseModel.AddMesure(captureDateTime, capturedValue, SensorTypeEnum.humidity);

                                             messageSignature[20] = '0';
                                             messageSignature[21] = capturedValue.ToString()[0];
                                             messageSignature[22] = capturedValue.ToString()[1];
                                             messageSignature[23] = capturedValue.ToString()[3];

                                             if (userSettings.isDiagnosticModeEnable)
                                                 lg.AppendLog(Log.CreateLog(String.Format("Air Humidity captured : {0}", capturedValue), LogType.Information));  
                                            }
                                             
                                        }).Wait();                                 
                                }
                                catch (Exception ex)
                                {
                                    lg.AppendLog(Log.CreateErrorLog("Exception on Air Temperature Humidity Sensor", ex));
                                    messageSignature[16] = 'C';
                                    messageSignature[17] = 'C';
                                    messageSignature[18] = 'C';
                                    messageSignature[19] = 'C';
                                    messageSignature[20] = 'C';
                                    messageSignature[21] = 'C';
                                    messageSignature[22] = 'C';
                                    messageSignature[23] = 'C';
                                }
                            }

                            try
                            {
                                var temp = internalTemperatureManager.GetInternalTemperatureSignature(); 
                                messageSignature[12] = temp[0];
                                messageSignature[13] = temp[1];
                                messageSignature[14] = temp[2];
                                messageSignature[15] = temp[3];               
                            }
                            catch (Exception ex)
                            {
                                lg.AppendLog(Log.CreateErrorLog("Exception on Internal Temperature Sensor", ex));
                                messageSignature[12] = 'C';
                                messageSignature[13] = 'C';
                                messageSignature[14] = 'C';
                                messageSignature[15] = 'C'; 
                            }

                        lg.AppendLog(Log.CreateLog(String.Format("Measures captured in {0} sec.", watchMesures.ElapsedMilliseconds / 1000), LogType.System));  
                        
                        if(userSettings.connectivityType == ConnectivityType.Sigfox && sigfoxManager.isInitialized && TICKSPERCYCLE >= 600000)
                        {
                            watchMesures.Restart();

                            Task.Run(async () =>
                            {
                                sigfoxManager.SendMessage(messageSignature.ToString(), userSettings.sigfoxVersion);
                                await Task.Delay(2000);    
                            }).Wait();

                            lg.AppendLog(Log.CreateLog(String.Format("Data sent to Azure via Sigfox in {0} sec.", watchMesures.ElapsedMilliseconds / 1000), LogType.System));
                        }

                        if (userSettings.connectivityType == ConnectivityType.Wifi && NetworkInterface.GetIsNetworkAvailable())
                        {
                            Task.Run(async () =>
                            {
                                await HttpClientHelper.SendMessage(userSettings.hubMessageAPI, 
                                                                    messageSignature.ToString(), 
                                                                    userSettings.productionSiteId);
                            }).Wait();                                       
                        }       
                    }
                }
                catch (Exception ex)
                {
                    lg.AppendLog(Log.CreateErrorLog("Exception on Measures", ex));
                    sigfoxManager.SendMessage("CCCCCCCCCCCCCCCCCCCCCCCC", userSettings.sigfoxVersion);
                }
            }
            watch.Stop();
        }
    }
}
