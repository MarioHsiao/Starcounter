using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace RapidMinds.BuildSystem.Common
{

    public class ExecuteCommandLineTool
    {

        #region Properties

        public string Command { get; protected set; }
        public string Arguments { get; protected set; }

        #endregion


        public void Execute(string command, string arguments)
        {
            if (string.IsNullOrEmpty(command)) throw new ArgumentException("Invalid command", "command");
            if (arguments == null) throw new ArgumentNullException("arguments", "Invalid arguments");

            this.Command = command;
            this.Arguments = arguments;

            this.StartExecutingCommand();
        }

        public void Execute(string command)
        {
            this.Execute(command, string.Empty);
        }

        private void StartExecutingCommand()
        {

            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            // Check if tool exists
            if (!File.Exists(this.Command))
            {
                throw new FileNotFoundException("Can not execute", this.Command);
            }

            processStartInfo.FileName = this.Command;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.Arguments = this.Arguments;

            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;

            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;

            Process process = new Process();
            process.StartInfo = processStartInfo;

            process.ErrorDataReceived += new DataReceivedEventHandler(process_ErrorDataReceived);
            process.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            int exitcode = process.ExitCode;

            process.Close();

            if (exitcode != 0)
            {
                throw new InvalidOperationException(string.Format("Error executing {0}, exitcode={1}", this.Command, exitcode));
            }

        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            Console.WriteLine(e.Data);
        }

        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            Console.WriteLine(e.Data);
        }

    }
}
