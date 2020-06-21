using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using myfoodapp.Core.Model;

namespace myfoodapp.Core.Business
{
    public class AtlasSensor
    {
        public SerialPort currentSerialPort;
        public SensorTypeEnum sensorType;

    }

    public sealed class AtlasSensorManager
    {
        //private LogModel logModel = LogModel.GetInstance;
        private List<AtlasSensor> sensorsList = new List<AtlasSensor>();

        public bool isInitialized = false;

        public enum CalibrationType
        {
            Mid,
            Low,
            High
        }

        private CancellationTokenSource ReadCancellationTokenSource;

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
        //private string queryCalibrationCommand = "Cal,?\r";
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
        //private string sleepModeCommand = "Sleep\r";
        private string readValueCommand = "R\r";
        private string setWaterTemperatureCommand = "T,{0}\r";
        //private string wakeupCommand = "W\r";
        private string disableContinuousModeCommand = "C,0\r";
        private string disableAutomaticAnswerCommand = "RESPONSE,0\r";

        //private string answersWrongCommand = "*ER";
        //private string answersOverVoltedCircuit = "*OV";
        //private string answersUnderVoltedCircuit = "*UV";
        //private string answersResetCircuit = "*RS";
        //private string answersBootUpCircuit = "*RE";
        private string answersSleepMode = "*SL";
        private string answersWakeUpMode = "*WA";

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

                //logModel.AppendLog(Log.CreateLog(String.Format("Sensors found in {0} sec.", watch.ElapsedMilliseconds / 1000), Log.LogType.System));

                for (int i = 0; i < ports.Count(); i++)
                {
                    try
                    {
                        if (ports[i].Contains("ttyUSB"))
                        {
                            // var task = Task.Run(async () =>
                            // {
                                try
                                {
                                    //logModel.AppendLog(Log.CreateLog(String.Format("Associating device ID - {0}", entry.Id), Log.LogType.System));

                                    var serialPort = new SerialPort(ports[i]);

                                    // Configure serial settings
                                    serialPort.BaudRate = 9600;
                                    serialPort.Parity = Parity.None;
                                    serialPort.StopBits = StopBits.One;
                                    serialPort.DataBits = 8;
                                    serialPort.Handshake = Handshake.None;
                                    serialPort.ReadTimeout = 5000;
                                    serialPort.WriteTimeout = 5000;

                                    // Create cancellation token object to close I/O operations when closing the device
                                    ReadCancellationTokenSource = new CancellationTokenSource();

                                    serialPort.DataReceived += SerialPortDataReceived;
                                    serialPort.Open();

                                    var newSensor = new AtlasSensor() { currentSerialPort = serialPort };

                                    string s = String.Empty;
                                    string r = String.Empty;
                                    string strResult = String.Empty;

                                    //var taskWakeUp = Task.Run(async () =>
                                    //{
                                    //    await WriteAsync(wakeupCommand, newSensor)
                                    //        .ContinueWith((a) => strResult = ReadAsync(ReadCancellationTokenSource.Token, newSensor).Result);

                                    //    await Task.Delay(1000);
                                    //});

                                    //taskWakeUp.Wait(20000);

                                    // var taskContinuous = Task.Run(async () =>
                                    // {
                                    //     await WriteAsync(disableContinuousModeCommand, newSensor);
                                    //     await Task.Delay(5000);
                                    // });
                                    // taskContinuous.Wait();

                                    // var taskStatus = Task.Run(async () =>
                                    // {
                                    //     await WriteAsync(getStatusCommand, newSensor)
                                    //          .ContinueWith((are) => s = ReadAsync(ReadCancellationTokenSource.Token, newSensor).Result);
                                    // });
                                    // taskStatus.Wait();

                                    serialPort.Write(informationCommand);
                                
                                    r = String.Empty;

                                    while(r == String.Empty)
                                    {
                                        if(serialPort.BytesToRead > 0)
                                        {
                                            r = serialPort.ReadExisting();
                                        }
                                    }

                                    // var taskInformation = Task.Run(async () =>
                                    // {
                                    //     await WriteAsync(informationCommand, newSensor)
                                    //          .ContinueWith((are) => r = ReadAsync(ReadCancellationTokenSource.Token, newSensor).Result);
                                    // });
                                    // taskInformation.Wait();

                                    // if (r.Contains("*OK"))
                                    // {
                                    //     var taskAuto = Task.Run(async () =>
                                    //     {
                                    //         await WriteAsync(disableAutomaticAnswerCommand, newSensor);
                                    //         await Task.Delay(5000);
                                    //     });
                                    //     taskAuto.Wait();
                                    // }

                                    //if (isSleepModeActivated)
                                    //{
                                    //    var taskSleep = Task.Run(async () =>
                                    //    {
                                    //        await WriteAsync(sleepModeCommand, newSensor);
                                    //    });

                                    //    taskSleep.Wait();
                                    //}

                                    //logModel.AppendLog(Log.CreateLog(String.Format("Sensor Information - {0}", r), Log.LogType.System));

                                    if (r.ToUpper().Contains("RTD"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.waterTemperature;
                                        //logModel.AppendLog(Log.CreateLog("Water Temperature online", Log.LogType.Information));
                                        //logModel.AppendLog(Log.CreateLog(String.Format("Water Temperature status - {0}", s), Log.LogType.System));
                                    }

                                    if (r.ToUpper().Contains("PH"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.pH;
                                        //logModel.AppendLog(Log.CreateLog("PH online", Log.LogType.Information));
                                        //logModel.AppendLog(Log.CreateLog(String.Format("PH status - {0}", s), Log.LogType.System));
                                    }

                                    if (r.ToUpper().Contains("ORP"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.ORP;
                                        //logModel.AppendLog(Log.CreateLog("ORP online", Log.LogType.Information));
                                        //logModel.AppendLog(Log.CreateLog(String.Format("ORP status - {0}", s), Log.LogType.System));
                                    }

                                    if (r.ToUpper().Contains("DO"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.dissolvedOxygen;
                                        //logModel.AppendLog(Log.CreateLog("Dissolved Oxygen online", Log.LogType.Information));
                                        //logModel.AppendLog(Log.CreateLog(String.Format("Dissolved Oxygen status - {0}", s), Log.LogType.System));
                                    }

                                    if (r.ToUpper().Contains("EC"))
                                    {
                                        newSensor.sensorType = SensorTypeEnum.EC;
                                        //logModel.AppendLog(Log.CreateLog("Electro-Conductivity online", Log.LogType.Information));
                                        //logModel.AppendLog(Log.CreateLog(String.Format("Electro-Conductivity - {0}", s), Log.LogType.System));
                                    }

                                    sensorsList.Add(newSensor);
                                }

                                catch (AggregateException ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                    //logModel.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
                                }
                            // });

                            // task.Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        //logModel.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                //logModel.AppendLog(Log.CreateErrorLog("Exception on Sensors Init", ex));
            }
            finally
            {
                isInitialized = true;

                //logModel.AppendLog(Log.CreateLog(String.Format("Atlas Sensors online in {0} sec.", watch.ElapsedMilliseconds / 1000), Log.LogType.System));
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
                string strResult = String.Empty;

                var taskCal = Task.Run(async () => {
                    await WriteAsync(calibrationCommand, currentSensor);
                });

                taskCal.Wait();
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

                var taskCal = Task.Run(async () => {
                    await WriteAsync(strQuery, currentSensor);
                });

                taskCal.Wait();
            }
        }

        public void ResetCalibration(SensorTypeEnum sensorType)
        {
            if (!isInitialized)
                return;

            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                string strResult = String.Empty;

                var taskCal = Task.Run(async () => {
                    await WriteAsync(clearCalibrationCommand, currentSensor);
                });

                taskCal.Wait();
            }
        }

        public void SetDebugLedMode(bool isEnable)
        {
            if (!isInitialized)
                return;

            var strIsEnable = isEnable ? "1" : "0";

            foreach (AtlasSensor currentSensor in sensorsList)
            {

                string strResult = String.Empty;

                var taskDebugMode = Task.Run(async () => {
                    await WriteAsync(String.Format(ledDebugCommand, strIsEnable), currentSensor);
                });

                taskDebugMode.Wait();
            }
        }

        public void ResetSensorsToFactory()
        {
            if (!isInitialized)
                return;

            foreach(AtlasSensor currentSensor in sensorsList)
            {
                string strResult = String.Empty;

                var taskReset = Task.Run(async () => {
                    await WriteAsync(String.Format(resetFactoryCommand), currentSensor);
                    await Task.Delay(5000);
                });

                taskReset.Wait();

                var taskAuto = Task.Run(async () =>
                {
                    await WriteAsync(disableAutomaticAnswerCommand, currentSensor);
                    await Task.Delay(5000);
                });
                taskAuto.Wait();

                var taskContinuous = Task.Run(async () =>
                {
                    await WriteAsync(disableContinuousModeCommand, currentSensor);
                    await Task.Delay(5000);
                });
                taskContinuous.Wait();

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

                var taskSetTemp = Task.Run(async () =>
                {
                    await WriteAsync(String.Format(setWaterTemperatureCommand, waterTemperature), phSensor);

                });

                taskSetTemp.Wait();

                return true;
            }

            return false;
        }

        public decimal RecordSensorsMeasure(SensorTypeEnum sensorType, bool isSleepModeActivated)
        {
            var currentSensor = this.GetSensor(sensorType);

            if (currentSensor != null)
            {
                decimal capturedMesure = 0;
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

                var taskMeasure = Task.Run(async () => {
                    await WriteAsync(readValueCommand, currentSensor)
                         .ContinueWith((a) => strResult = ReadAsync(ReadCancellationTokenSource.Token, currentSensor).Result);

                    var boolMeasure = Decimal.TryParse(strResult.Replace("\r", "")
                                                                .Replace(answersSleepMode, "")
                                                                .Replace(answersWakeUpMode, "")
                                                                , out capturedMesure);
                });

                taskMeasure.Wait();

                //if (isSleepModeActivated)
                //{
                //    var taskSleep = Task.Run(async () =>
                //    {
                //        await WriteAsync(sleepModeCommand, currentSensor);
                //    });

                //    taskSleep.Wait();
                //}

                return capturedMesure;
            }

            return 0;
        }

        public decimal RecordPhMeasure(bool isSleepModeActivated)
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

                //phSensor.currentSerialPort.DataReceived += SerialPortDataReceived;
                phSensor.currentSerialPort.Write(readValueCommand);

                for (int i = 0; i < 4; i++)
                {
                var r = String.Empty;

                while(r == String.Empty)
                {
                    if(phSensor.currentSerialPort.BytesToRead > 0)
                    {
                        r = phSensor.currentSerialPort.ReadLine();
                        Console.Write(r);
                    }
                }

                decimal capturedMesure = 0;

                var boolMeasure_1 = Decimal.TryParse(strResult.ToString().Replace("\r", "")
                                                                        .Replace(answersSleepMode, "")
                                                                        .Replace(answersWakeUpMode, ""), out capturedMesure);
                sumCapturedMesure += capturedMesure;


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

        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;
        
            // Read the data that's in the serial buffer.
            var serialdata = serialPort.ReadExisting();
        
            // Write to debug output.
            Console.WriteLine(serialdata);
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

                var taskStatus = Task.Run(async () => {
                    await WriteAsync(getStatusCommand, currentSensor)
                         .ContinueWith((a) => strResult = ReadAsync(ReadCancellationTokenSource.Token, currentSensor).Result);

                });

                taskStatus.Wait();

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

        private async Task<string> WriteAsync(string command, AtlasSensor currentSensor)
        {
            Task<UInt32> storeAsyncTask;

            currentSensor.currentSerialPort.Write(command);

            return String.Empty;
        }

        private async Task<string> ReadAsync(CancellationToken cancellationToken, AtlasSensor currentSensor)
        {

                return currentSensor.currentSerialPort.ReadLine();

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
