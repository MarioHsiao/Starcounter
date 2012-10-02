using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RapidMinds.BuildSystem.Common;

namespace Plugin.SpeedGrid.Tasks
{
    public class BuildSpeedGridSolution
    {
        #region Properties

        public string Executable { get; protected set; }
        public string SolutionFile { get; protected set; }

        #endregion

        public BuildSpeedGridSolution(string executable, string solutionFile)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(solutionFile)) throw new ArgumentException("Invalid solutionFile", "solutionFile");

            this.Executable = executable;
            this.SolutionFile = solutionFile;
        }

        public void Execute()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine();
            Console.Error.WriteLine("------ Build started: Building SpeedGrid solution ------");
            Console.ResetColor();

            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();

            StringBuilder arguments = new StringBuilder();
            arguments.Append(this.AssureCorrectPath(this.SolutionFile));
            arguments.Append(" ");
            arguments.Append("/Build");
            arguments.Append(" ");
            arguments.Append("Release");

            executeTask.Execute(this.Executable, arguments.ToString());

            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Build succeeded.");
            Console.ResetColor();

        }

        private String AssureCorrectPath(string path)
        {

            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                return path;
            }

            return string.Format("\"{0}\"", path);
        }
    }
}
