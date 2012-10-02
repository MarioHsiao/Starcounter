using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{


    // SET VERSION=1.2.3.5
    // SET SETVERSIONTOOL="c:\perforce\Starcounter\Dev\Yellow\BuildSystemCode\SetAssemblyVersion\bin\Release\SetVersionNumber.exe"
    // SET VERSIONASSEMBLY="c:\tmp\TestBuild\RapidMinds\SpeedGrid\SpeedGrid\Properties\AssemblyInfo.cs"


    public class SetVersonNumberToSourceFiles
    {
        #region Properties

        public string Executable { get; protected set; }
        public string AssemblyFile { get; protected set; }
        public Version Version { get; protected set; }

        #endregion

        public SetVersonNumberToSourceFiles(Version version, string executable, string assemblyFile)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(assemblyFile)) throw new ArgumentException("Invalid assemblyFile", "assemblyFile");

            this.Version = version;
            this.Executable = executable;
            this.AssemblyFile = assemblyFile;

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
            Console.Error.WriteLine("------ Modify started: Modifying version number on source files ------");
            Console.ResetColor();


            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();

            //executeTask.Complete += new ExecuteCommandLineTool.CompleteEventHandler(executeTask_Complete);
            //executeTask.Progress += new ExecuteCommandLineTool.ProgressEventHandler(executeTask_Progress);

            StringBuilder arguments = new StringBuilder();
            arguments.Append("/F");
            arguments.Append(" ");
            arguments.Append(this.AssureCorrectPath(this.AssemblyFile));
            arguments.Append(" ");
            arguments.Append(this.Version);


            executeTask.Execute(this.Executable, arguments.ToString());

            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Modify succeeded.");
            Console.ResetColor();

        }

        //void executeTask_Progress(object sender, ProgressEventArgs e)
        //{
        //    Console.WriteLine(e.Text);
        //}

        //void executeTask_Complete(object sender, CompletedEventArgs e)
        //{
        //    Console.WriteLine("executeTask_Complete");

        //    if (e.HasError)
        //    {
        //        throw e.Error;
        //    }


        //}



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
