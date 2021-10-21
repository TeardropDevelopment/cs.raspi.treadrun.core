using System;
using TreadSense.Device;
using TreadSense.Helpers;
using System.IO;
using TreadSense.Calibration;
using System.Threading.Tasks;
using TreadSense.Threads;
using Newtonsoft.Json;
using TreadSense.Service;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;
using System.Net.Sockets;
using System.Configuration;
using System.Net;

namespace TreadSense
{
    class Program
    {
        public static string DIRECTORY = "../treadsense";
        public static string FILENAME = "device.json";

        //Entry point in the program/service
        static void Main(string[] args)
        {
            /*
             * args[0] can be:  (Which device is the user using)
             *  - TreadRun.ZeroW
             *
             * args[1] can be:  (is the User some special person? (TreadRun+ user, Beta tester, developer))
             *  - Default
             *  - Plus
             *  - Develop
             *  - Beta
             */

            LogCenter.Instance.LogInfo("Welcome to TreadSense! Starting...");

            InitializeProgram();

            DeviceSettings device = null;

            try
            {
                //read from file
                DeviceJson deviceObj = JsonConvert.DeserializeObject<DeviceJson>(File.ReadAllText($"{DIRECTORY}/{FILENAME}"));

                if (deviceObj != null)
                {
                    device = new DeviceSettings(string.Format("TreadSense.{0}", Pi.Info.RaspberryPiVersion), Helper.StringToEnum<DeviceType>(deviceObj.DeviceType), false);
                    LogCenter.Instance.LogInfo(string.Format(I18n.Translation.DeviceCreated, device.DeviceName));
                }
                else
                {
                    LogCenter.Instance.LogError("deviceObj == null | " + File.ReadAllText($"{DIRECTORY}/{FILENAME}"));
                }

            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogFatalError(ex.Message);
                Console.ReadKey();
                return;
            }

            //Initialize device
            InitializeUser(device);
            LogCenter.Instance.LogInfo("User initialized | Start device thread");

            // Start server
            TcpListener listener = null;

            try
            {
                listener = new TcpListener(IPAddress.Any, int.Parse(ConfigurationManager.AppSettings["port"]));
                listener.Start();

                LogCenter.Instance.LogInfo("Server ready to accept requests...");

                while (true)
                {
                    var c = listener.AcceptTcpClient();

                    Task.Run(async () => await DeviceThread.StartAsync(c));
                }

            }
            finally
            {

            }

        }

        #region static methods

        private static void InitializeUser(DeviceSettings device)
        {
            LogCenter.Instance.LogInfo(device);

            switch (device.DeviceType)
            {
                case DeviceType.Default:
                    device.RegisterCalibration(new VelocityCalibration());
                    break;
                case DeviceType.Plus:
                case DeviceType.Beta:
                case DeviceType.Develop:
                    device.RegisterCalibration(new VelocityCalibration());
                    device.RegisterCalibration(new InclineCalibration());
                    break;
                default:
                    device.RegisterCalibration(new VelocityCalibration());
                    break;
            }

            device.SetInitialized(true);

            User.Instance.SetDevice(device);
        }

        private static void InitializeProgram()
        {
            //Init RaspberryIO
            Pi.Init<BootstrapWiringPi>();

            try
            {
                //Create folders and files the first time...
                if (!Directory.Exists("DIRECTORY"))
                {
                    Directory.CreateDirectory(DIRECTORY);
                }

                if (!File.Exists($"{DIRECTORY}/{FILENAME}"))
                {
                    File.WriteAllText($"{DIRECTORY}/{FILENAME}", "{\"deviceType\":\"Develop\"}");
                }
            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogError(ex.Message);
            }

        }

#endregion
    }

#region json classes

    public class CalibrationJson
    {
        [JsonProperty("isCalibrated")]
        public bool IsCalibrated { get; set; }

        [JsonProperty("averageDistance")]
        public int AverageDistance { get; set; }

        [JsonProperty("defaultIncline")]
        public int DefaultIncline { get; set; }
    }

    public class DeviceJson
    {
        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("calibration")]
        public CalibrationJson Calibration { get; set; }

        public override string ToString()
        {
            return DeviceType + " " + Calibration;
        }
    }

#endregion

}
