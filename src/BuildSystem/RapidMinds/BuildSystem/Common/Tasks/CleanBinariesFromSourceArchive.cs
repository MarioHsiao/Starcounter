using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class CleanBinariesFromSourceArchive
    {
        #region Properties

        public string Executable { get; protected set; }
        public string SolutionFile { get; protected set; }

        #endregion

        public CleanBinariesFromSourceArchive(string executable, string solutionFile)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(solutionFile)) throw new ArgumentException("Invalid solutionFile", "solutionFile");

            this.Executable = executable;
            this.SolutionFile = solutionFile;
        }

        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Clean started: Cleaning binaries from source archive ------");
            Console.ResetColor();

            ExecuteCommandLineTool cleanTask = new ExecuteCommandLineTool();


            StringBuilder arguments = new StringBuilder();
            arguments.Append(this.AssureCorrectPath(this.SolutionFile));
            arguments.Append(" ");
            arguments.Append("/Clean");
            arguments.Append(" ");
            arguments.Append("Release");

            cleanTask.Execute(this.Executable, arguments.ToString());


            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Clean succeeded.");
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
