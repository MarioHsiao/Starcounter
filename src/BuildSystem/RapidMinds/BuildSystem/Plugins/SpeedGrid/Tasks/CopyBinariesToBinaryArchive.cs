using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RapidMinds.BuildSystem.Common;
using RapidMinds.BuildSystem.Common.Tools;

namespace Plugin.SpeedGrid.Tasks
{
    public class CopyBinariesToBinaryArchive
    {
        #region Properties

        public Version Version { get; protected set; }

        public string BinaryArchive { get; protected set; }
        public string SourceArchive { get; protected set; }
        public string SerialInformation { get; protected set; }

        public VersionInfo VersionInfo { get; protected set; }
        public DateTime VersionCreation { get; protected set; }

        #endregion

        public CopyBinariesToBinaryArchive(Version version, string binaryArchive, string sourceArchive, string serialInformation, DateTime versionCreation)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(binaryArchive)) throw new ArgumentException("Invalid binaryArchive", "binaryArchive");
            if (string.IsNullOrEmpty(sourceArchive)) throw new ArgumentException("Invalid sourceArchive", "sourceArchive");
            if (string.IsNullOrEmpty(serialInformation)) throw new ArgumentException("Invalid serialInformation", "serialInformation");

            this.Version = version;
            this.BinaryArchive = binaryArchive;
            this.SourceArchive = sourceArchive;
            this.SerialInformation = serialInformation;
            this.VersionCreation = versionCreation;

        }

        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Copy started: Copy binaries to binary archive ------");
            Console.ResetColor();



            // Add Version as a Folder
            string outputfolder = Path.Combine(this.BinaryArchive, this.Version.ToString());

            // Add SerialInformation as a folder
            outputfolder = Path.Combine(outputfolder, this.SerialInformation);

            // Assure output folder
            if (!Directory.Exists(outputfolder))
            {
                Directory.CreateDirectory(outputfolder);
            }

            #region Copy Installer .EXE

            string artifactPath = Path.Combine(this.SourceArchive, this.Version.ToString());

            artifactPath = Path.Combine(artifactPath, "RapidMinds");
            artifactPath = Path.Combine(artifactPath, "SpeedGrid");
            artifactPath = Path.Combine(artifactPath, "Setup Installer");
            artifactPath = Path.Combine(artifactPath, "Setup Installer");
            artifactPath = Path.Combine(artifactPath, "Express");
            artifactPath = Path.Combine(artifactPath, "SingleImage");
            artifactPath = Path.Combine(artifactPath, "DiskImages");
            artifactPath = Path.Combine(artifactPath, "DISK1");

            string[] files = Directory.GetFiles(artifactPath);

            int filesCopied = 0;

            foreach (string file in files)
            {
                string destFile = Path.Combine(outputfolder, Path.GetFileName(file));
                if (file.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Copy files from {0} to {1}", file, destFile);
                    File.Copy(file, destFile, true);
                    filesCopied++;
                }
            }

            if (filesCopied == 0)
            {
                Directory.Delete(outputfolder, true); // Roolback
                throw new FileNotFoundException("Can not find generated installer EXE.", artifactPath);
            }
            #endregion

            #region Copy Changelog

            int changeLogFileCopied = 0;

            string changeLogFile = Path.Combine(this.SourceArchive, this.Version.ToString());
            changeLogFile = Path.Combine(changeLogFile, "RapidMinds");
            changeLogFile = Path.Combine(changeLogFile, "SpeedGrid");
            changeLogFile = Path.Combine(changeLogFile, "SpeedGrid");
            changeLogFile = Path.Combine(changeLogFile, string.Format("changelog-{0}.txt", this.Version.ToString()));

            if (File.Exists(changeLogFile))
            {
                string destChangeLogFile = Path.Combine(outputfolder, Path.GetFileName(changeLogFile));
                File.Copy(changeLogFile, destChangeLogFile, true);
                changeLogFileCopied++;
            }
            #endregion

            #region Copy SpeedGrid .NET 3.5 .DLL

            string speedGridDllPath = Path.Combine(this.SourceArchive, this.Version.ToString());
            speedGridDllPath = Path.Combine(speedGridDllPath, "RapidMinds");
            speedGridDllPath = Path.Combine(speedGridDllPath, "SpeedGrid");
            speedGridDllPath = Path.Combine(speedGridDllPath, "SpeedGrid");
            speedGridDllPath = Path.Combine(speedGridDllPath, "bin");
            speedGridDllPath = Path.Combine(speedGridDllPath, "Release");

            int speedGridFilesCopied = 0;

            string[] speedGridFiles = Directory.GetFiles(speedGridDllPath);


            foreach (string file in speedGridFiles)
            {
                string destFile = Path.Combine(outputfolder, Path.GetFileName(file));

                if (file.EndsWith(".SpeedGrid.dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Copy files from {0} to {1}", file, destFile);
                    File.Copy(file, destFile, true);
                    speedGridFilesCopied++;
                }
            }

            if (speedGridFilesCopied == 0)
            {
                Directory.Delete(outputfolder, true); // Roolback
                throw new FileNotFoundException("Can not find generated SpeedGrid DLL.", speedGridDllPath);
            }

            #endregion

            #region Copy SpeedGrid .NET 4.0 .DLL

            string speedGrid4DllPath = Path.Combine(this.SourceArchive, this.Version.ToString());
            speedGrid4DllPath = Path.Combine(speedGrid4DllPath, "RapidMinds");
            speedGrid4DllPath = Path.Combine(speedGrid4DllPath, "SpeedGrid");
            speedGrid4DllPath = Path.Combine(speedGrid4DllPath, "RapidMinds.Controls.Wpf4.SpeedGrid");
            speedGrid4DllPath = Path.Combine(speedGrid4DllPath, "bin");
            speedGrid4DllPath = Path.Combine(speedGrid4DllPath, "Release");

            int speedGrid4FilesCopied = 0;

            string[] speedGrid4Files = Directory.GetFiles(speedGrid4DllPath);


            foreach (string file in speedGrid4Files)
            {
                string destFile = Path.Combine(outputfolder, Path.GetFileName(file));

                if (file.EndsWith(".SpeedGrid.dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Copy files from {0} to {1}", file, destFile);
                    File.Copy(file, destFile, true);
                    speedGrid4FilesCopied++;
                }
            }

            if (speedGrid4FilesCopied == 0)
            {
                Directory.Delete(outputfolder, true); // Roolback
                throw new FileNotFoundException("Can not find generated SpeedGrid DLL.", speedGrid4DllPath);
            }

            #endregion

            #region Copy Class Documentation API

            // From source archve to binary archive
            string docFolder = Path.Combine(this.SourceArchive, this.Version.ToString());
            docFolder = Path.Combine(docFolder, "RapidMinds");
            docFolder = Path.Combine(docFolder, "SpeedGrid");
            docFolder = Path.Combine(docFolder, "Documentation");
            docFolder = Path.Combine(docFolder, "ClassAPI");

            if (Directory.Exists(docFolder))
            {
                string destDocFolder = Path.Combine(this.BinaryArchive, this.Version.ToString());
                destDocFolder = Path.Combine(destDocFolder, "Documentation");
                destDocFolder = Path.Combine(destDocFolder, "ClassAPI");

                Utils.CopyDirectory(docFolder, destDocFolder, ref filesCopied);

            }


            #endregion

            // Create and Save VersionInfo_<counter>.xml
            this.AssureVersionInfo();

            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Copy succeeded, {0} files copied.", changeLogFileCopied + speedGridFilesCopied + filesCopied);
            Console.ResetColor();

        }



        private void AssureVersionInfo()
        {
            // Create VersionInfo_<counter>.xml

            VersionInfo versionInfo = new VersionInfo();
            versionInfo.Version = this.Version.ToString();
            versionInfo.IDTailBase64 = this.SerialInformation;
            versionInfo.IDTailDecimal = this.SerialInformation; // TODO: Make Integer?
            versionInfo.VersionCreation = this.VersionCreation;

            DateTime d1 = new DateTime(2011, 01, 01);
            DateTime d2 = DateTime.Now;
            TimeSpan t1 = d2 - d1;
            int seconds = (int)t1.TotalSeconds;


            string fileName = string.Format("VersionInfo_{0}.xml", seconds.ToString());
            string versionInfoFile = Path.Combine(this.BinaryArchive, this.Version.ToString());
            versionInfoFile = Path.Combine(versionInfoFile, fileName);

            VersionInfo.Save(versionInfo, versionInfoFile);

            this.VersionInfo = versionInfo;

        }



    }

}
