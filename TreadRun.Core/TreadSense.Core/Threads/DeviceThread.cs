using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TreadSense.Calibration;
using TreadSense.Device;
using TreadSense.Helpers;
using TreadSense.Services;

namespace TreadSense.Threads
{
    class DeviceThread : Exchange
    {
        private static string EXCHANGEKEY = ConfigurationManager.AppSettings["exchangeKey"];
        private static bool ISRUNNING = false;

        public static async Task StartAsync(object o)
        {
            LogCenter.Instance.LogInfo("Started thread...");

            //// CAlibration is later a user problem
            //if(!CalibrationService.Instance.VelocityCalibration.IsCalibrated)
            //{
            //    if(CalibrationService.Instance.VelocityCalibration.Calibrate())
            //        LogCenter.Instance.LogInfo("Calibration successful!");
            //} 
            //else
            //{
            //    LogCenter.Instance.LogInfo("Device already calibrated!");
            //}

            //if (!CalibrationService.Instance.InclineCalibration.IsCalibrated)
            //{
            //    if (CalibrationService.Instance.InclineCalibration.Calibrate())
            //        LogCenter.Instance.LogInfo("Calibration successful!");
            //}
            //else
            //{
            //    LogCenter.Instance.LogInfo("Device already calibrated!");
            //}
            #region connect to server

            NetworkStream ns = null;
            BinaryReader br = null;
            BinaryWriter bw = null;
            TcpClient client = null;

            try
            {

                client = o as TcpClient;

                LogCenter.Instance.LogInfo("Client connected...");

                //Get streams
                ns = client.GetStream();
                br = new BinaryReader(ns);
                bw = new BinaryWriter(ns);

                #region WELCOME MSG

                LogCenter.Instance.LogInfo("Start welcome MSG");
                var welcomeAction = ReadAction(br);

                if (welcomeAction != null)
                {
                    WelcomeJSON welcomeJSON = new WelcomeJSON();
                    if (EnsureExchangeKey(welcomeAction.ExchangeKey))
                    {
                        welcomeJSON.Status = (int)Enums.EnResponseCode.OK;
                        welcomeJSON.ExchangeKey = EXCHANGEKEY;
                        welcomeJSON.IsValidDevice = true;
                        welcomeJSON.IsCalibrated = CalibrationService.Instance.IsCalibrated();
                        welcomeJSON.Version = ConfigurationManager.AppSettings["version"];
                        welcomeJSON.DeviceType = User.Instance.DeviceSettings.DeviceType.ToString();
                    }
                    else
                    {
                        welcomeJSON.ExchangeKey = EXCHANGEKEY;
                        welcomeJSON.Status = (int)Enums.EnResponseCode.BADREQUEST;
                    }
                    Send(welcomeJSON, bw);
                }

                #endregion


                LogCenter.Instance.LogInfo("Start loop...");

                ActionJSON action;
                while (true)
                {

                    action = ReadAction(br);

                    if (ISRUNNING)
                    {

                    } 
                    else
                    {
                        if (action.Action.Equals(Enums.EnActions.Start.ToString()))
                        {
                            #region Start action

                            StartJSON startJSON = new StartJSON();
                            if (EnsureExchangeKey(action.ExchangeKey))
                            {
                                if (CalibrationService.Instance.IsCalibrated())
                                {
                                    startJSON.Status = (int)Enums.EnResponseCode.OK;
                                    startJSON.ExchangeKey = EXCHANGEKEY;
                                }
                                else
                                {
                                    startJSON.Status = (int)Enums.EnResponseCode.NOTCALIBRATED;
                                    startJSON.ExchangeKey = EXCHANGEKEY;
                                }
                            }
                            else
                            {
                                startJSON.ExchangeKey = EXCHANGEKEY;
                                startJSON.Status = (int)Enums.EnResponseCode.BADREQUEST;
                            }
                            Send(startJSON, bw);

                            #endregion
                        }

                        if (action.Action.Equals(Enums.EnActions.Calibrate.ToString()))
                        {
                            #region calibrate action
                            CalibrateAndStopJSON calibrateJSON = new CalibrateAndStopJSON();
                            if (EnsureExchangeKey(calibrateJSON.ExchangeKey))
                            {
                                calibrateJSON = CalibrationService.Instance.CalibrateDevice();
                                calibrateJSON.ExchangeKey = EXCHANGEKEY;
                                calibrateJSON.Status = (int)Enums.EnResponseCode.OK;
                            }
                            else
                            {
                                calibrateJSON.ExchangeKey = EXCHANGEKEY;
                                calibrateJSON.Status = (int)Enums.EnResponseCode.BADREQUEST;
                            }
                            Send(calibrateJSON, bw);

                            #endregion
                        }
                    }

                    await Task.Delay(1);
                }


                #endregion
            }
            catch (Exception ex)
            {
                LogCenter.Instance.LogFatalError(ex.Message);
                LogCenter.Instance.LogFatalError(ex.StackTrace);
                throw;
            }
            finally
            {
                ns?.Close();
                br?.Close();
                bw?.Close();
                client?.Close();
            }
        }

        #region private methods

        private static ActionJSON ReadAction(BinaryReader br)
        {
            return JsonConvert.DeserializeObject<ActionJSON>(Recv(br));
        }

        private static string Recv(BinaryReader br)
        {
            try
            {
                string s = br.ReadString();
                LogCenter.Instance.LogInfo(" <= " + s);
                return s;
            }
            catch (Exception)
            {
                // DO NOTHING HERE (EOS ... End Of Stream)
            }

            return String.Empty;
        }

        private static void Send(object o, BinaryWriter bw)
        {
            LogCenter.Instance.LogInfo(" => " + JsonConvert.SerializeObject(o));
            bw.Write(JsonConvert.SerializeObject(o));
        }

        private static bool EnsureExchangeKey(string key)
        {
            return EXCHANGEKEY.Equals(key);
        }

        #endregion
    }
}
