using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class BuildClassAPIDocumentation
    {

        // Command: c:\FTP\SCDev\BuildSystem\InstallshieldMod.exe "%teamcity.build.checkoutDir%\Setup Installer\Setup Installer.isl" "%env.BUILD_NUMBER%" "%env.RAMDISK%\"

        public Version Version { get; protected set; }
        public string Executable { get; protected set; }
        public string ProjectFile { get; protected set; }
        public string SourceArchive { get; protected set; }
        public string SolutionName { get; protected set; }

        public BuildClassAPIDocumentation(Version version, string executable, string projectFile, string sourceArchive, string solutionName )
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(projectFile)) throw new ArgumentException("Invalid projectFile", "projectFile");
            if (string.IsNullOrEmpty(sourceArchive)) throw new ArgumentException("Invalid sourceArchive", "sourceArchive");
            if (string.IsNullOrEmpty(solutionName)) throw new ArgumentException("Invalid solutionName", "solutionName");

            this.Version = version;
            this.Executable = executable;
            this.ProjectFile = projectFile;
            this.SourceArchive = sourceArchive;
            this.SolutionName = solutionName;
        }

        public void Execute()
        {


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Build started: Building Class API Documentation ------");
            Console.ResetColor();








            // DXROOT=C:\Program Files (x86)\Sandcastle\
            // SHFBROOT=C:\Program Files (x86)\EWSoftware\Sandcastle Help File Builder\

            // PATH
            // C:\Python27\
            // C:\Program Files (x86)\HTML Help Workshop
            // C:\Program Files (x86)\Common Files\Microsoft Shared\Help 2.0 Compiler

            // DXROOT=c:\FTP\SCDev\BuildSystem\RapidMinds\SpeedGrid\Sandcastle
            // SHFBROOT=c:\FTP\SCDev\BuildSystem\RapidMinds\SpeedGrid\SandcastleHelpFileBuilder\

            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();

            string outputFolder = Path.Combine(this.SourceArchive, this.Version.ToString());

            outputFolder = Path.Combine(outputFolder, "RapidMinds");
            outputFolder = Path.Combine(outputFolder, this.SolutionName);
            outputFolder = Path.Combine(outputFolder, "Documentation");
            outputFolder = Path.Combine(outputFolder, "ClassAPI");


            // C:\RapidMinds\SpeedGrid\SourceArchive\StableBuilds\0.0.90.0\RapidMinds\SpeedGrid\SpeedGrid\bin\Debug\RapidMinds.Controls.SpeedGrid.dll

            // Check if Class API is lready generated
            if (Directory.Exists(outputFolder))
            {
                // Only build once!!
                // Time Elapsed 00:00:00.72
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Error.WriteLine("Build skipped, Class API Documentation already exist.");
                Console.ResetColor();
                return;
            }


            StringBuilder arguments = new StringBuilder();
            arguments.Append("/p:OutputPath=" + outputFolder);
            arguments.Append(" ");
            arguments.Append("/p:HelpFileFormat=Website");
            arguments.Append(" ");
            arguments.Append(this.AssureCorrectPath(this.ProjectFile));
            arguments.Append(" ");
            arguments.Append("/p:Configuration=Release");



            // c:\perforce\RapidMinds\SpeedGrid\Documentation>msbuild /p:OutputPath=bin\Debug\ SpeedGrid.shfbproj


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
