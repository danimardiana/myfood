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
    public class AtlasSensor
    {
        public SerialPort currentSerialPort;
        public SensorTypeEnum sensorType;
    }

    public sealed class AtlasSensorManager
    {
        private LogManager lg = LogManager.GetInstance;
        private List<AtlasSensor> sensorsList = new List<AtlasSensor>();

        public bool isInitialized = false;

        public enum CalibrationType
        {
            Mid,
            Low,
            High
        }

        private static AtlasSensorManager instance;

        public static AtlasSensorManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AtlasSensorManager();
                }
                return instance;
            }
        }

        //pH Sensors Commands
        private string queryCalibrationCommand = "Cal,?\r";
        private string clearCalibrationCommand = "Cal,clear\r";
        private string midCalibrationCommand = "Cal,mid,7.00\r";
        private string lowCalibrationCommand = "Cal,low,4.00\r";
        private string highCalibrationCommand = "Cal,high,10.00\r";
        private string setCalibrationCommand = "Cal,{0}\r";

        //Water Temperature Sensors commands
        //private string setCelsiusTemperatureCommand = "S,C\r";
        //private string setFahrenheitTemperatureCommand = "S,F\r";
        //private string setKelvinTemperatureCommand = "S,K\r";

        private string informationCommand = "I\r";
        private string resetFactoryCommand = "Factory\r";
        private string ledDebugCommand = "L,{0}\r";
        private string getStatusCommand = "Status\r";
        private string sleepModeCommand = "Sleep\r";
        private string readValueCommand = "R\r";
        private string setWaterTemperatureCommand = "T,{0}\r";
        private string wakeupCommand = "W\r";
        private string getContinuousModeStatus = "C,?\r";
        private string disableContinuousModeCommand = "C,0\r";
        private string answersContinuousModeOn = "?C,1";
        private string getAutomaticAnswerStatusv1 = "RESPONSE,?\r";
        private string disableAutomaticAnswerCommandv1 = "RESPONSE,0\r";
        private string enableAutomaticAnswerCommandv1 = "RESPONSE,1\r";  
        private string answersAutomaticAnswerOnv1 = "?RESPONSE,1\r";    
        private string getAutomaticAnswerStatusv2 = "OK,?\r";
        private string disableAutomaticAnswerCommandv2 = "OK,0\r";
        private string enableAutomaticAnswerCommandv2 = "OK,1\r";
        private string answersAutomaticAnswerOnv2 = "?OK,1\r";
        private string answersWrongCommand = "*ER";
        private string answersOverVoltedCircuit = "*OV";
        private string answersUnderVoltedCircuit = "*UV";
        private string answersResetCircuit = "*RS";
        private string answersBootUpCircuit = "*RE";
        private string answersSleepMode = "*SL";
        private string answersWakeUpMode = "*WA";

        string s = String.Empty;
        string r = String.Empty;
        string strResult = String.Empty;

        private AtlasSensorManager()
        {
        }

        public void InitSensors(bool isSleepModeActivated)
        {
            if (isInitialized)
                return;

            var watch = Stopwatch.StartNew();

            try
            {
                string[] ports = SerialPort.GetPortNames();

                lg.AppendLog(Log.CreateLog(String.Format("Sensors found in {0} sec.", watch.ElapsedMilliseconds / 1000), LogType.System));

                for (int i = 0; i < ports.Count(); i++)
                {
                    try
                    {
                        if (ports[i].Contains("ttyUSB"))
                        {
                                try
                                {
                                    lg.AppendLog(Log.CreateLog(String.Format("Associating device ID - {0}", ports[i]), LogType.System));

                                    var serialPort = new SerialPort(ports[i]);

                                    // Configure serial settings
                                    serialPort.BaudRate = 9600;
                                    serialPort.Parity = Parity.None;
                                    serialPort.StopBits = StopBits.One;
                                    serialPort.DataBits = 8;
                                    serialPort.Handshake = Handshake.None;
                                    serialPort.ReadTimeout = 6000;
                                    serialPort.WriteTimeout = 6000;

                                    serialPort.Open();     

                                    var tsk = Task.Run(async() =>
                                     {
                                        r = await SendCommand(serialPort, informationCommand);
                                     });
                                    tsk.Wait();             

                                    tsk = Task.Run(async() =>
                                     {
                                        s = await SendCommand(serialPort, getStatusCommand);                                  
                                     });
                                    tsk.Wait();                       

                                    tsk = Task.Run(async() =>
                                     {
                                        strResult = await SendCommand(serialPort, getContinuousModeStatus);                                      
                                     });
                                    tsk.Wait(); 
                                                                   
                                    if(strResult.Contains(answersContinuousModeOn))
                                    {
                                        tsk = Task.Run(async() =>
                                        {
                                            strResult = await SendCommand(serialPort, disableContinuousModeCommand);                                          
                                        });
                                     tsk.Wait();  
                                            
                                    }                                  

                                    tsk = Task.Run(async() =>
                                     {
                                        strResult = await SendCommand(serialPort, getAutomaticAnswerStatusv1);       
                                     });
                                    tsk.Wait();    
                                                                    

                                    if(strResult.Contains(answersAutomaticAnswerOnv1))
                                    {
                                        tsk = Task.Run(async() =>
                                        {
                                            strResult = await SendCommand(serialPort, disableAutomaticAnswerCommandv1);
                                        });
                                        tsk.Wait();  
                                    }

                                    tsk = Task.Run(async() =>
                                     {
                                        strResult = await SendCommand(serialPort, getAutomaticAnswerStatusv2);
                                     });
                                    tsk.Wait(); 
                                                      
                                    if(strResult.Contains(answersAutomaticAnswerOnv2))
                                    {
                                        tsk = Task.Run(async() =>
                                        {
                                            strResult = await SendCommand(serialPort, disableAutomaticAnswerCommandv2);
                                        });
                                        tsk.Wait();                                                          
                                    }  

                                    //if (isSleepModeActivated)
                                    //{
                                    //    var taskSleep = Task.Run(async () =>
                                    //    {
                                    //        await WriteAsync(sleepModeCommand, newSensor);
                                    //    });

                                    //    taskSleep.Wait();
                                    //}

                                    var newSensor = new AtlasSensor() { currentSerialPort = serialPort };

                                    lg.AppendLog(Log.CreateLog(String.Format("Sensor Information - {0}", r), LogType.System));

                                    if (r.ToUpper().Contains("RTD"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.waterTemperature;
                                        lg.AppendLog(Log.CreateLog("Water Temperature online", LogType.Information));
                                        lg.AppendLog(Log.CreateLog(String.Format("Water Temperature status - {0}", s), LogType.System));
                                    }

                                    if (r.ToUpper().Contains("PH"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.pH;
                                        lg.AppendLog(Log.CreateLog("PH online", LogType.Information));
                                        lg.AppendLog(Log.CreateLog(String.Format("PH status - {0}", s), LogType.System));
                                    }

                                    if (r.ToUpper().Contains("ORP"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.ORP;
                                        lg.AppendLog(Log.CreateLog("ORP online", LogType.Information));
                                        lg.AppendLog(Log.CreateLog(String.Format("ORP status - {0}", s), LogType.System));
                                    }

                                    if (r.ToUpper().Contains("DO"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.dissolvedOxygen;
                                        lg.AppendLog(Log.CreateLog("Dissolved Oxygen online", LogType.Information));
                                        lg.AppendLog(Log.CreateLog(String.Format("Dissolved Oxygen status - {0}", s), LogType.System));
                                    }

                                    if (r.ToUpper().Contains("EC"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.EC;
                                        lg.AppendLog(Log.CreateLog("Electro-Conductivity online", LogType.Information));
                                        lg.AppendLog(Log.CreateLog(String.Format("Electro-Conductivity - {0}", s), LogType.System));
                                    }

                                    sensorsList.Add(newSensor);
                                }

                                catch (AggregateException ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                    lg.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        lg.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                lg.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
            }
            finally
            {
                isInitialized = true;

                lg.AppendLog(Log.CreateLog(String.Format("Atlas Sensors online in {0} sec.", watch.ElapsedMilliseconds / 1000), LogType.System));
                watch.Stop();
            }
        }

        public bool isSensorOnline(SensorTypeEnum currentSensorType)
        {
            return (sensorsList.Where(s => s.sensorType == currentSensorType).FirstOrDefault() != null) ? true : false;
        }

        public AtlasSensor GetSensor(SensorTypeEnum currentSensorType)
        {
            return sensorsList.Where(s => s.sensorType == currentSensorType).FirstOrDefault();
        }

        public void SetCalibration(SensorTypeEnum sensorType, CalibrationType calibrationType)
        {
            if (!isInitialized)
                return;

            var calibrationCommand = String.Empty;

            switch (calibrationType)
            {
                case CalibrationType.Mid:
                    calibrationCommand = midCalibrationCommand;
                    break;
                case CalibrationType.Low:
                    calibrationCommand = lowCalibrationCommand;
                    break;
                case CalibrationType.High:
                    calibrationCommand = highCalibrationCommand;
                    break;
                default:
                    return;
            }

            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                var tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSensor.currentSerialPort, calibrationCommand);
                });
                tsk.Wait(); 
            }
        }

        public void SetCalibration(SensorTypeEnum sensorType, int calibrationValue)
        {
            if (!isInitialized)
                return;

            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                string strQuery = String.Format(setCalibrationCommand, calibrationValue);

                var tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSensor.currentSerialPort, strQuery);
                });
                tsk.Wait(); 
            }
        }

        public void ResetCalibration(SensorTypeEnum sensorType)
        {
            if (!isInitialized)
                return;

            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                var tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSensor.currentSerialPort, clearCalibrationCommand);
                });
                tsk.Wait();              
            }
        }

        public void SetDebugLedMode(bool isEnable)
        {
            if (!isInitialized)
                return;

            var strIsEnable = isEnable ? "1" : "0";

            foreach (AtlasSensor currentSensor in sensorsList)
            {
                var tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSensor.currentSerialPort, String.Format(ledDebugCommand, strIsEnable));
                });
                tsk.Wait();
            }
        }

        public void ResetSensorsToFactory()
        {
            if (!isInitialized)
                return;

            foreach(AtlasSensor currentSensor in sensorsList)
            {
                var currentSerialPort = currentSensor.currentSerialPort;

                var tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSerialPort, resetFactoryCommand);
                });
                tsk.Wait();

                tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSerialPort, disableAutomaticAnswerCommandv1);
                });
                tsk.Wait();

                tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSerialPort, disableAutomaticAnswerCommandv2);
                });
                tsk.Wait();

                tsk = Task.Run(async() =>
                {
                    await SendCommand(currentSerialPort, disableContinuousModeCommand);
                });
                tsk.Wait();
            }
        }

        public bool SetWaterTemperatureForPHSensor(decimal waterTemperature)
        {
            var phSensor = this.GetSensor(SensorTypeEnum.pH);

            if (phSensor != null)
            {
                //var taskWakeUp = Task.Run(async () =>
                //{
                //    await WriteAsync(wakeupCommand, phSensor);
                //});

                //taskWakeUp.Wait();

                var tsk = Task.Run(async() =>
                {
                    await SendCommand(phSensor.currentSerialPort, String.Format(setWaterTemperatureCommand, waterTemperature));
                });
                tsk.Wait();

                return true;
            }
            return false;
        }

        public decimal RecordSensorsMeasure(SensorTypeEnum sensorType, bool isSleepModeActivated)
        {
            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                StringBuilder strResult = new StringBuilder();
                decimal sumCapturedMesure = 0;

                //if (isSleepModeActivated)
                //{
                //    var taskWakeUp = Task.Run(async () =>
                //    {
                //        await WriteAsync(wakeupCommand, currentSensor);

                //        await Task.Delay(1000);
                //    });

                //    taskWakeUp.Wait();
                //}

                for (int i = 0; i < 4; i++)
                {
                    Task.Run(async() =>
                    {
                        var str = await SendCommand(currentSensor.currentSerialPort, readValueCommand);
                        strResult.Append(str);
                    }).Wait();

                    decimal capturedMesure = 0;
                    var boolMeasure = Decimal.TryParse(strResult.ToString().Replace("\r", "")
                                                            .Replace(answersSleepMode, "")
                                                            .Replace(answersWakeUpMode, "")
                                                            ,out capturedMesure);

                    sumCapturedMesure += capturedMesure;
                    strResult.Clear();          
                }

                //if (isSleepModeActivated)
                //{
                //    var taskSleep = Task.Run(async () =>
                //    {
                //        await WriteAsync(sleepModeCommand, currentSensor);
                //    });

                //    taskSleep.Wait();
                //}

                return sumCapturedMesure / 4;
            }
            return 0;
        }

        public decimal RecordpHMeasure(bool isSleepModeActivated)
        {
            var phSensor = this.GetSensor(SensorTypeEnum.pH);

            if (phSensor != null)
            {
                StringBuilder strResult = new StringBuilder();
                decimal sumCapturedMesure = 0;

                //if (isSleepModeActivated)
                //{
                //    var taskWakeUp = Task.Run(async () =>
                //    {
                //        await WriteAsync(wakeupCommand, phSensor);
                //        await Task.Delay(1000);
                //    });

                //    taskWakeUp.Wait();
                //}

                for (int i = 0; i < 4; i++)
                {
                    Task.Run(async() =>
                    {
                        var str = await SendCommand(phSensor.currentSerialPort, readValueCommand);
                        strResult.Append(str);
                    }).Wait();
                    
                    decimal capturedMesure = 0;

                    var boolMeasure_1 = Decimal.TryParse(strResult.ToString().Replace("\r", "")
                                                                            .Replace(answersSleepMode, "")
                                                                            .Replace(answersWakeUpMode, ""), out capturedMesure);
                    sumCapturedMesure += capturedMesure;
                    strResult.Clear();
                }

                //if (isSleepModeActivated)
                //{
                //    var taskSleep = Task.Run(async () =>
                //    {
                //        await WriteAsync(sleepModeCommand, phSensor);
                //    });

                //    taskSleep.Wait();
                //}

                return sumCapturedMesure / 4;
            }

            return 0;
        }

        public string GetSensorStatus(SensorTypeEnum sensorType, bool isSleepModeActivated)
        {
            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                string strResult = String.Empty;

                //if (isSleepModeActivated)
                //{
                //    var taskWakeUp = Task.Run(async () =>
                //    {
                //        await WriteAsync(wakeupCommand, currentSensor);

                //        await Task.Delay(1000);
                //    });

                //    taskWakeUp.Wait();
                //}

                var tsk = Task.Run(async() =>
                {
                    strResult = await SendCommand(currentSensor.currentSerialPort, getStatusCommand);
                });
                tsk.Wait();

                //if (isSleepModeActivated)
                //{
                //    var taskSleep = Task.Run(async () =>
                //    {
                //        await WriteAsync(sleepModeCommand, currentSensor);
                //    });

                //    taskSleep.Wait();
                //}

                return strResult;
            }

            return string.Empty;
        }

        private async Task<string> SendCommand(SerialPort serialPort, string command)
        {
            string result = string.Empty;

            serialPort.WriteLine(command);
            await Task.Delay(1000);
            result = serialPort.ReadExisting();

            Console.WriteLine(result);
            return result;                                
        }

        private void CloseDevice(AtlasSensor sensor)
        {
            if (sensor.currentSerialPort != null)
            {
                sensor.currentSerialPort.Dispose();
            }
            sensorsList = null;
        }
    }
}
