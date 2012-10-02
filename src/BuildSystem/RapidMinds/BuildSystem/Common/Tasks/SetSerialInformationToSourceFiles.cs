using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RapidMinds.BuildSystem.Common.Tasks
{

    public class SetSerialInformationToSourceFiles
    {
        #region Properties

        public string Executable { get; protected set; }
        public string AssemblyFile { get; protected set; }
        public string SerialInformation { get; protected set; }

        #endregion

        public SetSerialInformationToSourceFiles(string executable, string assemblyFile, string serialInformation)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(assemblyFile)) throw new ArgumentException("Invalid assemblyFile", "assemblyFile");
            if (string.IsNullOrEmpty(serialInformation)) throw new ArgumentException("Invalid serialInformation", "serialInformation");

            this.Executable = executable;
            this.AssemblyFile = assemblyFile;
            this.SerialInformation = serialInformation;
        }

        /// <summary>
        /// Executes the specified tool.
        /// </summary>
        /// <param name="tool">FullPath to the SetVersionNumbertool</param>
        /// <param name="assemblyFile">FullPath to the assebly file to modify.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        public void Execute()
        {


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Modify started: Modifying serial information on source files ------");
            Console.ResetColor();


            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();

            //executeTask.Complete += new ExecuteCommandLineTool.CompleteEventHandler(executeTask_Complete);
            //executeTask.Progress += new ExecuteCommandLineTool.ProgressEventHandler(executeTask_Progress);

            StringBuilder arguments = new StringBuilder();
            arguments.Append("/S");
            arguments.Append(" ");
            arguments.Append(this.AssureCorrectPath(this.AssemblyFile));
            arguments.Append(" ");
            arguments.Append(this.SerialInformation);

            // , "== Modifying Assembly SerialInformation =="
            executeTask.Execute(this.Executable, arguments.ToString());


            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Modify succeeded.");
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
