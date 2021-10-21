using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TreadSense.Helpers;
using static Exchange;
using System.Configuration;
using TreadSense.Enums;
using Newtonsoft.Json;

namespace TreadSense.Service
{
    public class RunService
    {

        #region threadsafe singleton

        private static volatile RunService _instance;
        private static readonly object SyncRoot = new object();

        public static RunService Instance
        {
            [DebuggerStepThrough]
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new RunService();
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region private members

        private Calculations.Calculation c;
        private Stopwatch timeBetweenStripes;

        private bool readOld, readNew = true;

        #endregion

        private RunService()
        {
            c = new Calculations.Calculation();
            timeBetweenStripes = new Stopwatch();
        }
        

        public void Start()
        {
            timeBetweenStripes.Restart();
        }

        public void Stop()
        {
            timeBetweenStripes.Stop();
        }

        public void CalculateAndSend(BinaryWriter bw)
        {
            // Read the stripes and calculate the distance between them
            readOld = readNew;
            readNew = GPIOHelper.ReadDigital(17);
            if (readOld != readNew && readNew)
            {
                if (timeBetweenStripes.ElapsedMilliseconds >= 10)
                {
                    var v = c.CalculateVelocity(timeBetweenStripes.Elapsed.TotalSeconds);
                    LogCenter.Instance.LogFatal(v);
                    Send(bw, v);
                    timeBetweenStripes.Restart();
                }
            }
        }

        #region private methods

        private void Send(BinaryWriter bw, double v)
        {
            StartJSON obj = new StartJSON();
            obj.ExchangeKey = ConfigurationManager.AppSettings["exchangeKey"];
            obj.Status = (int)EnResponseCode.OK;
            obj.Velocity = (float)v;
            obj.Spm = 0;
            obj.Incline = 0;

            bw.Write(JsonConvert.SerializeObject(obj));
        }

        #endregion

    }
}
