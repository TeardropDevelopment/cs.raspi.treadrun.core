using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreadSense.Helpers
{
    class Bash
    {

        public delegate void OnOutputDataHandler(object source, DataReceivedEventArgs e);
        public static event OnOutputDataHandler OnOutputData;

        public static string ExecuteBashCommand(string command)
        {
            command = command.Replace("\"", "\\\"");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            return proc.StandardOutput.ReadToEnd();
        }

        public static void ExecuteBashCommandWithEvents(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            process.ErrorDataReceived += Process_OutputDataReceived;
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (OnOutputData != null)
            {
                OnOutputData(sender, e);
            }
        }
    }
}
