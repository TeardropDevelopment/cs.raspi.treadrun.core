﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using TreadRun.Core.Extensions;
using TreadRun.Core.Device;
using TreadRun.Core.Helpers;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TreadRun.Core.Calibration;

namespace TreadRun.Core
{
    class Program
    {

        public static string DIRECTORY = "../treadrun";
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


            InitializeProgram();

            DeviceSettings device = null;

            try
            {
                //read from file
                DeviceJson deviceObj = JsonConvert.DeserializeObject<DeviceJson>(File.ReadAllText($"{DIRECTORY}/{FILENAME}"));

                device = new DeviceSettings(deviceObj.DeviceName, Helper.StringToEnum<DeviceType>(deviceObj.DeviceType), deviceObj.Calibration.IsCalibrated);
                LogCenter.Instance.LogInfo(string.Format(I18n.Translation.DeviceCreated, device.DeviceName));

            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogError(ex.Message);
                Console.ReadKey();
                return;
            }

            //Initialize device
            InitializeUser(device);
            LogCenter.Instance.LogInfo("User initialized | Start device thread");

            Console.ReadKey();
        }

        private static void InitializeUser(DeviceSettings device)
        {
            switch (device.DeviceType)
            {
                case DeviceType.Default:
                    device.RegisterCalibration(new VelocityCalibration());
                    break;
                case DeviceType.Plus:
                case DeviceType.Beta:
                case DeviceType.Develop:
                    device.RegisterCalibration(new VelocityCalibration());
                    break;
                default:
                    device.RegisterCalibration(new VelocityCalibration());
                    break;
            }

            device.SetInitialized(true);

            User.Initialize(device);
        }

        private static void InitializeProgram()
        {
            LogCenter.Initialize();

            try
            {
                //Create folders and files the first time...
                if (!Directory.Exists("DIRECTORY"))
                {
                    Directory.CreateDirectory(DIRECTORY);
                }

                if (!File.Exists($"{DIRECTORY}/{FILENAME}"))
                {
                    File.WriteAllText($"{DIRECTORY}/{FILENAME}", "{\"deviceName\":\"TreadRun.ZeroW\",\"deviceType\":\"Default\",\"calibration\":{\"isCalibrated\":false,\"averageDistance\":0,\"defaultIncline\":0}}");
                }
            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogError(ex.Message);
            }

        }
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
        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("calibration")]
        public CalibrationJson Calibration { get; set; }
    }

    #endregion

}
