using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TreadSense.Calibration;
using TreadSense.Device;
using TreadSense.Helpers;
using TreadSense.I18n;
using TreadSense.Service;

namespace TreadSense.Threads
{
    class ActionChangedEA : EventArgs
    {
        public Exchange.ActionJSON Action { get; set;}

        public ActionChangedEA(Exchange.ActionJSON action)
        {
            Action = action;
        }
    }

    class DeviceThread : Exchange
    {
        private static string EXCHANGEKEY = ConfigurationManager.AppSettings["exchangeKey"];
        private static bool ISRUNNING = false;

        private static new ActionJSON ActionJSON = null;
        private static event EventHandler<ActionChangedEA> ActionChanged;

        public static Task StartAsync(object o) {

            #region EventListener

            ActionChanged += DeviceThread_ActionChanged;

            #endregion

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
                ReadActionAsync(br);

                while (ActionJSON == null);
                var welcomeAction = ActionJSON;

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
                else
                {
                    var temp = new WelcomeJSON();
                    temp.Status = (int)Enums.EnResponseCode.BADREQUEST;
                    temp.ExchangeKey = EXCHANGEKEY;
                    Send(temp, bw);

                    LogCenter.Instance.LogError("False protocol. Terminate...");
                    return Task.CompletedTask;
                }

                #endregion

                Thread.Sleep(1);
                LogCenter.Instance.LogInfo("Start loop...");
                GPIOHelper.Initialize();

                ActionJSON = null;
                ActionJSON action = null;
                while (true)
                {
                    if (ActionJSON != null)
                    {
                        action = ActionJSON;
                        LogCenter.Instance.LogInfo("<= New action incoming: " + action.Action);
                    }

                    if (ISRUNNING && action == null)
                    {
                        RunService.Instance.CalculateAndSend(bw);
                    }

                    if (action != null)
                    {
                        if (action.Action.Equals(Enums.EnActions.Start.ToString()))
                        {
                            #region Start action

                            StartJSON startJSON = new StartJSON();
                            if (EnsureExchangeKey(action.ExchangeKey) && !ISRUNNING)
                            {
                                if (CalibrationService.Instance.IsCalibrated())
                                {
                                    startJSON.Status = (int)Enums.EnResponseCode.OK;
                                    startJSON.ExchangeKey = EXCHANGEKEY;

                                    RunService.Instance.Start();
                                    ISRUNNING = true;
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
                            if (EnsureExchangeKey(action.ExchangeKey))
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

                        if (action.Action.Equals(Enums.EnActions.Stop.ToString()))
                        {
                            #region Start action

                            CalibrateAndStopJSON stopJSON = new CalibrateAndStopJSON();
                            if (EnsureExchangeKey(action.ExchangeKey))
                            {
                                stopJSON.ExchangeKey = EXCHANGEKEY;
                                stopJSON.Status = (int)Enums.EnResponseCode.OK;

                                ISRUNNING = false;
                                RunService.Instance.Stop();
                            }
                            else
                            {
                                stopJSON.ExchangeKey = EXCHANGEKEY;
                                stopJSON.Status = (int)Enums.EnResponseCode.BADREQUEST;
                            }
                            Send(stopJSON, bw);

                            #endregion
                        }


                        action = null;
                        ActionJSON = null;
                    }

                    Thread.Sleep(1);
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

        private static void DeviceThread_ActionChanged(object sender, ActionChangedEA e)
        {
            ActionJSON = e.Action;
            LogCenter.Instance.LogInfo("New Action setted = " + e.Action);
        }

        private static void ReadActionAsync(BinaryReader br)
        {

            BackgroundWorker bw = new BackgroundWorker() { WorkerSupportsCancellation = true };

            bw.DoWork += Bw_DoWork;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;

            bw.RunWorkerAsync(br);
        }

        private static void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                LogCenter.Instance.LogInfo("BackgroundWorker canceled");
            }
        }

        private static void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                string s = Recv(e.Argument as BinaryReader);
                ActionChanged(e, new ActionChangedEA(string.IsNullOrEmpty(s) ? null : JsonConvert.DeserializeObject<ActionJSON>(s)));
                Thread.Sleep(1);
            }
        }

        private static string Recv(BinaryReader br)
        {
            try
            {
                string s = br.ReadString();
                LogCenter.Instance.LogInfo("<= " + s);
                return s;
            }
            catch (Exception)
            {
                // DO NOTHING HERE (EOS ... End Of Stream)
                //LogCenter.Instance.LogError("EOFException");
            }

            return string.Empty;
        }

        private static void Send(object o, BinaryWriter bw)
        {
            LogCenter.Instance.LogInfo("=> " + JsonConvert.SerializeObject(o));
            bw.Write(JsonConvert.SerializeObject(o));
        }

        private static bool EnsureExchangeKey(string key)
        {
            return EXCHANGEKEY.Equals(key);
        }

        #endregion
    }
}
