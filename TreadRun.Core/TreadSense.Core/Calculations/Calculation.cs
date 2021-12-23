using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TreadSense.Helpers;
using TreadSense.Service;

namespace TreadSense.Calculations
{
    public class Calculation
    {
        #region private members

        private double currentMps;

        // Velocity
        private List<double> distances;
        private int index = 0;

        // Default incline
        private double defaultIncline;

        #endregion

        #region ctor

        public Calculation()
        {
            distances = CalibrationService.Instance.GetDistances();
        }

        #endregion

        #region Calculate

        /// <summary>
        /// Calculates the meter per seconds for the next stripe
        /// </summary>
        /// <param name="time">The time between stripes</param>
        /// <returns><double>double</double> mps</returns>
        public double CalculateVelocity(double time)
        {
            try
            {
                currentMps = distances[index].ToFixed(2) / time.ToFixed(0);

                if (++index >= distances.Count)
                {
                    index = 0;
                }

                return currentMps;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int CalculateIncline(double currentActualIncline, double offset)
        {
            try
            {
                if(currentActualIncline >= defaultIncline - offset && currentActualIncline <= defaultIncline + offset)
                {
                    return (int)currentActualIncline - (int)defaultIncline;
                }

                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

    }
}
