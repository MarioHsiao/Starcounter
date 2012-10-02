using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RapidMinds.BuildSystem.Common;
using RapidMinds.BuildSystem.Common.Tools;

namespace Plugin.DragTabControl.Tasks
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
            artifactPath = Path.Combine(artifactPath, "DragTabControl");
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
            changeLogFile = Path.Combine(changeLogFile, "DragTabControl");
            changeLogFile = Path.Combine(changeLogFile, "RapidMinds.Controls.Wpf35.DragTabControl");
            changeLogFile = Path.Combine(changeLogFile, string.Format("changelog-{0}.txt", this.Version.ToString()));

            if (File.Exists(changeLogFile))
            {
                string destChangeLogFile = Path.Combine(outputfolder, Path.GetFileName(changeLogFile));
                File.Copy(changeLogFile, destChangeLogFile, true);
                changeLogFileCopied++;
            }
            #endregion

            #region Copy DragTabControl .NET 3.5 .DLL

            string assemblyPath = Path.Combine(this.SourceArchive, this.Version.ToString());
            assemblyPath = Path.Combine(assemblyPath, "RapidMinds");
            assemblyPath = Path.Combine(assemblyPath, "DragTabControl");
            assemblyPath = Path.Combine(assemblyPath, "RapidMinds.Controls.Wpf35.DragTabControl");
            assemblyPath = Path.Combine(assemblyPath, "bin");
            assemblyPath = Path.Combine(assemblyPath, "Release");

            int assemblyFilesCopied = 0;

            string[] assemblyFiles = Directory.GetFiles(assemblyPath);


            foreach (string file in assemblyFiles)
            {
                string destFile = Path.Combine(outputfolder, Path.GetFileName(file));

                if (file.EndsWith(".DragTabControl.dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Copy files from {0} to {1}", file, destFile);
                    File.Copy(file, destFile, true);
                    assemblyFilesCopied++;
                }
            }

            if (assemblyFilesCopied == 0)
            {
                Directory.Delete(outputfolder, true); // Roolback
                throw new FileNotFoundException("Can not find generated DragTabControl DLL.", assemblyPath);
            }

            #endregion

            #region Copy DragTabControl .NET 4.0 .DLL

            string assembly4DllPath = Path.Combine(this.SourceArchive, this.Version.ToString());
            assembly4DllPath = Path.Combine(assembly4DllPath, "RapidMinds");
            assembly4DllPath = Path.Combine(assembly4DllPath, "DragTabControl");
            assembly4DllPath = Path.Combine(assembly4DllPath, "RapidMinds.Controls.Wpf4.DragTabControl");
            assembly4DllPath = Path.Combine(assembly4DllPath, "bin");
            assembly4DllPath = Path.Combine(assembly4DllPath, "Release");

            int assembly4FilesCopied = 0;

            string[] assembly4Files = Directory.GetFiles(assembly4DllPath);


            foreach (string file in assembly4Files)
            {
                string destFile = Path.Combine(outputfolder, Path.GetFileName(file));

                if (file.EndsWith(".DragTabControl.dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Copy files from {0} to {1}", file, destFile);
                    File.Copy(file, destFile, true);
                    assembly4FilesCopied++;
                }
            }

            if (assembly4FilesCopied == 0)
            {
                Directory.Delete(outputfolder, true); // Roolback
                throw new FileNotFoundException("Can not find generated DragTabControl DLL.", assembly4DllPath);
            }

            #endregion

            #region Copy Class Documentation API

            // From source archve to binary archive
            string docFolder = Path.Combine(this.SourceArchive, this.Version.ToString());
            docFolder = Path.Combine(docFolder, "RapidMinds");
            docFolder = Path.Combine(docFolder, "DragTabControl");
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
            Console.Error.WriteLine("Copy succeeded, {0} files copied.", changeLogFileCopied + assemblyFilesCopied + filesCopied);
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
