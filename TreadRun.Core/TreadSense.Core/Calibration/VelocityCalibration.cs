using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TreadSense.Helpers;

namespace TreadSense.Calibration
{
    class VelocityCalibration : ICalibration
    {
        //Speed used to calibrate the device (kph)
        private const int KPH = 5;

        //How long should the calibration wait for the first stripe (sec)
        private const int TIMEOUT = 30;

        //How long should the calibration run (sec)
        private const int CALIBRATIONTIME = 15;

        //Photresistor GPIO pin
        private const int PR_GPIO = 17;

        public List<double> Distance { get; private set; }

        public bool IsCalibrated { get; private set; }

        /// <summary>
        /// Starts the calibartion of the treadmill and the device
        /// Routine:
        ///     User starts treadmill to 5 kph
        ///     Wait for the first photoResistor input
        ///     Count time between the two inputs and store the distance
        ///     Run 60 seconds then the <double>AverageDeltaTime</double> should be accurate enough
        /// </summary>
        /// <returns>True, if the calibration was a success</returns>
        public bool Calibrate()
        {
            LogCenter.Instance.LogInfo("Start calibrating velocity");

            //vars
            int hits = 0;
            bool readOld = true, readNew;
            List<double> distances = new List<double>();

            //start timer
            Stopwatch runTime = new Stopwatch();
            Stopwatch timeBetweenStripes = new Stopwatch();

            runTime.Start();
            while (GPIOHelper.ReadDigital(PR_GPIO))
            {
                if (runTime.Elapsed.TotalSeconds >= TIMEOUT)
                {
                    LogCenter.Instance.LogError("408 | Calibration ran into a timeout");
                    return false;
                }

                Thread.Sleep(1);
            }

            // Read the stripes and calculate the distance between them
            runTime.Restart();
            timeBetweenStripes.Restart();
            double lastTime = 0;
            while (runTime.Elapsed.TotalSeconds <= CALIBRATIONTIME)
            {
                // If another stripe got read
                readNew = GPIOHelper.ReadDigital(PR_GPIO);
                if ((readOld != readNew) && !(readOld = readNew))
                {
                    if (timeBetweenStripes.ElapsedMilliseconds - lastTime >= 50)
                    {
                        var time = timeBetweenStripes.Elapsed.TotalMilliseconds.ToFixed(0);
                        distances.Add(((time - lastTime) * (KPH / 3.6)).ToFixed(0));
                        lastTime = time;
                        LogCenter.Instance.LogInfo("Stripe hitted!");

                        hits++;
                    }
                }
                Thread.Sleep(1);
            }

            if (distances.Count > 0)
            {
                LogCenter.Instance.LogError("400 | Calibration ran into an error");
                return false;
            }

            //Calculate how many stripes there are
            List<double> s = new List<double>();
            bool foundStripes = false;
            int k = 0;
            for (int i = 0; i < distances.Count; i++)
            {
                if (s.Count > 0 && s[k] == distances[i])
                {
                    if (k++ == 3)
                    {
                        foundStripes = true;
                        break;
                    }
                }
                s.Add(distances[i]);
                Thread.Sleep(1);
            }
            if(foundStripes)
                s.RemoveRange(s.Count - 4, 3);

            Distance = s;
            foreach (var item in Distance)
            {
                LogCenter.Instance.LogInfo(item);
                Thread.Sleep(1);
            }

            IsCalibrated = true;

            Save();

            return true;
        }

        #region load / save

        public object Load()
        {
            try
            {
                string serialized = File.ReadAllText($"{Program.DIRECTORY}/velcalibration.json");
                VelocityCalibrationJSON obj = JsonConvert.DeserializeObject<VelocityCalibrationJSON>(serialized);
                IsCalibrated = obj.IsCalibrated;
                Distance = obj.Distance;
            }
            catch (Exception)
            {
                LogCenter.Instance.LogError("[VelocityCalibration] Device wasn't calibrated before!");
            }

            return Distance;
        }

        public void Save()
        {
            VelocityCalibrationJSON obj = new VelocityCalibrationJSON();
            obj.Distance = Distance;
            obj.IsCalibrated = IsCalibrated;

            try
            {
                string serialized = JsonConvert.SerializeObject(obj);
                File.WriteAllText($"{Program.DIRECTORY}/velcalibration.json", serialized);
            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogError(ex);
            }
        }

#endregion

    }

#region JSON classes

    public class VelocityCalibrationJSON
    {
        [JsonProperty("isCalibrated")]
        public bool IsCalibrated { get; set; }

        [JsonProperty("distance")]
        public List<double> Distance { get; set; }
    }

#endregion

}
