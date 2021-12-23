using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace TreadSense.Helpers
{
    class GPIOHelper
    {
        private static Dictionary<int, bool> gpios = new Dictionary<int, bool>();

        /// <summary>
        /// Initialize the GPIO helpers and start the python script
        /// </summary>
        public static void Initialize()
        {
            new Thread(() => { 
                Console.WriteLine("GPIOHelper#Initialize");
                gpios[17] = false;
                Bash.OnOutputData += Bash_OnOutputData;
                Bash.ExecuteBashCommandWithEvents("python3 ../../../../readPin.py");
            }).Start();
        }

        private static void Bash_OnOutputData(object source, System.Diagnostics.DataReceivedEventArgs e)
        {
            lock (gpios)
            {
                if (bool.TryParse(e.Data, out bool result) && result != gpios[17])
                {
                    gpios[17] = !gpios[17];
                }
            }
        }

        ///// <summary>
        ///// Set a GPIO as an output
        ///// </summary>
        ///// <param name="pin">GPIO ID</param>
        //public static void SetAsOutput(int pin)
        //{
        //    if(gpios.ContainsKey(pin))
        //    {
        //        gpios[pin].PinMode = GpioPinDriveMode.Output;
        //    }
        //    else
        //    {
        //        IGpioPin gpio = Pi.Gpio[pin];
        //        gpio.PinMode = GpioPinDriveMode.Output;
        //        gpios.Add(pin, gpio);
        //    }
        //}

        ///// <summary>
        ///// Set a GPIO as an output
        ///// </summary>
        ///// <param name="pin">GPIO ID</param>
        //public static void SetAsOutput(BcmPin pin)
        //{
        //    SetAsOutput(pin);
        //}

        ///// <summary>
        ///// Set a GPIO as an input
        ///// </summary>
        ///// <param name="pin">GPIO ID</param>
        //public static void SetAsInput(int pin)
        //{
        //    if (gpios.ContainsKey(pin))
        //    {
        //        gpios[pin].PinMode = GpioPinDriveMode.Input;
        //    }
        //    else
        //    {
        //        IGpioPin gpio = Pi.Gpio[pin];
        //        gpio.PinMode = GpioPinDriveMode.Input;
        //        gpios.Add(pin, gpio);
        //    }
        //}

        ///// <summary>
        ///// Set a GPIO as an input
        ///// </summary>
        ///// <param name="pin">GPIO ID</param>
        //public static void SetAsInput(BcmPin pin)
        //{
        //    SetAsInput(pin);
        //}

        /// <summary>
        /// Reads the state of a GPIO
        /// </summary>
        /// <param name="gpioPin">GPIO ID</param>
        /// <returns>True, if high</returns>
        public static bool ReadDigital(int gpioPin)
        {
            try
            {
                //SetAsInput(gpioPin);
                return gpios[gpioPin];
            }
            catch (Exception)
            {
                return false;
            }
        }

        ///// <summary>
        ///// Reads the state of a GPIO
        ///// </summary>
        ///// <param name="gpioPin">GPIO ID</param>
        ///// <returns>True, if high</returns>
        //public static bool ReadDigital(BcmPin gpioPin)
        //{
        //    return ReadDigital(gpioPin);
        //}

        ///// <summary>
        ///// Writes a digital signal 5V / 0V
        ///// </summary>
        ///// <param name="gpioPin">GPIO ID</param>
        ///// <param name="high">True = 5V | False = 0V</param>
        //public static void WriteDigital(int gpioPin, bool high = true)
        //{
        //    SetAsOutput(gpioPin);
        //    gpios[gpioPin].Write(high);
        //}

        ///// <summary>
        ///// Writes a digital signal 5V / 0V
        ///// </summary>
        ///// <param name="gpioPin">GPIO ID</param>
        ///// <param name="high">True = 5V | False = 0V</param>
        //public static void WriteDigital(BcmPin gpioPin, bool high = true)
        //{
        //    WriteDigital(gpioPin, high);
        //}
    }
}
