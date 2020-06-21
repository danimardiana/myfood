using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using myfoodapp.Core.Common;

namespace myfoodapp.Core.Business
{
    public sealed class SigfoxInterfaceManager
    {
        private LogManager lg = LogManager.GetInstance;
        public bool isInitialized = false;
        private SerialPort serialPort;

        private CancellationTokenSource ReadCancellationTokenSource;

        private static SigfoxInterfaceManager instance;

        public static SigfoxInterfaceManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SigfoxInterfaceManager();
                }
                return instance;
            }
        }

        private string sendMessageATCommand = "AT$SF={0},1\r";
        private string isOnlineATCommand = "AT\r";
        private string setEchoOFFATCommand = "ATE0\r";

        private SigfoxInterfaceManager()
        {         
        }

        public void InitSensors()
        {
            if (isInitialized)
                return;

            var watch = Stopwatch.StartNew();

            try
            {
                string[] ports = SerialPort.GetPortNames();

                var sigfoxPortName = ports.ToList().Where(s => s.Contains(@"ttyAMA0")).FirstOrDefault();
                
                serialPort = new SerialPort(sigfoxPortName);

                if (serialPort != null)
                    lg.AppendLog(Log.CreateLog("Sigfox device not found", LogType.System));
                    try
                    {
                                try
                                {
                                    lg.AppendLog(Log.CreateLog("Associating Sigfox device", LogType.System));

                                    // Configure serial settings
                                    serialPort.BaudRate = 9600;
                                    serialPort.Parity = Parity.None;
                                    serialPort.StopBits = StopBits.One;
                                    serialPort.DataBits = 8;
                                    serialPort.Handshake = Handshake.None;
                                    serialPort.ReadTimeout = 5000;
                                    serialPort.WriteTimeout = 5000;

                                    serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                                    serialPort.Open(); 

                                    serialPort.WriteLine(isOnlineATCommand);                            
                                }

                                catch (AggregateException ex)
                                {
                                    lg.AppendLog(Log.CreateErrorLog("Exception on Sigfox Init", ex));
                                }
                    }
                    catch (Exception ex)
                    {
                        lg.AppendLog(Log.CreateErrorLog("Exception on Sigfox Init", ex));
                    }              
            }
            catch (Exception ex)
            {
                lg.AppendLog(Log.CreateErrorLog("Exception on Sigfox Init", ex));
            }
            finally
            {
                isInitialized = true;

                lg.AppendLog(Log.CreateLog(String.Format("Sigfox Interface online in {0} sec.", watch.ElapsedMilliseconds / 1000), LogType.System));
                watch.Stop();
            }
        }

        public void SendMessage(string message)
        {
            if (!isInitialized)
                return;

                string strResult = String.Empty;

                serialPort.WriteLine(String.Format(sendMessageATCommand, message));
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.Write(indata);
        }

    }
}
