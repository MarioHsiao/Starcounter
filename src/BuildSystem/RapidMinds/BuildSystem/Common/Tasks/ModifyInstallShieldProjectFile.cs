using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class ModifyInstallShieldProjectFile
    {

        // Command: c:\FTP\SCDev\BuildSystem\InstallshieldMod.exe "%teamcity.build.checkoutDir%\Setup Installer\Setup Installer.isl" "%env.BUILD_NUMBER%" "%env.RAMDISK%\"

        public string Executable { get; protected set; }
        public string ProjectFile { get; protected set; }
        public string SourceArchive { get; protected set; }
        public Version Version { get; protected set; }
        public string SerialInformation { get; protected set; }
        public string ProjectName { get; protected set; }

        public ModifyInstallShieldProjectFile(Version version, string serialInformation, string executable, string projectFile, string sourceArchive, string projectname)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(serialInformation)) throw new ArgumentException("Invalid serialInformation", "serialInformation");
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(projectFile)) throw new ArgumentException("Invalid projectFile", "projectFile");
            if (string.IsNullOrEmpty(sourceArchive)) throw new ArgumentException("Invalid sourceArchive", "sourceArchive");
            if (string.IsNullOrEmpty(projectname)) throw new ArgumentException("Invalid projectname", "projectname");

            this.Version = version;
            this.SerialInformation = serialInformation;
            this.Executable = executable;
            this.ProjectFile = projectFile;
            this.SourceArchive = sourceArchive;
            this.ProjectName = projectname;
        }

        public void Execute()
        {


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Modify started: Modifying installshield projectfile ------");
            Console.ResetColor();


            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();

            StringBuilder arguments = new StringBuilder();
            arguments.Append(this.AssureCorrectPath(this.ProjectFile));
            arguments.Append(" ");
            arguments.Append(this.Version);
            arguments.Append(" ");
            arguments.Append(this.SerialInformation);
            arguments.Append(" ");
            arguments.Append(this.AssureCorrectPath(Path.Combine(this.SourceArchive, this.Version.ToString())));
            arguments.Append(" ");
            arguments.Append(this.ProjectName);


            // C:\Perforce\rapidminds\speedgrid\
            // C:\Perforce\

            // c:\tmp\RapidMinds\SpeedGrid\SourceArchive\Nightlybuilds\1.0.27.0\


            // "c:\perforce\RapidMinds\SpeedGrid\Resources\Grid (32x32).ico" => "<sourepath>\Resources\Grid (32x32).ico"

            // , "== Modifying InstallShield Projectfile =="
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
