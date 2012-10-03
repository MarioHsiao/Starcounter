using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class UploadInstanceToFTPServer
    {

        #region Properties

        public VersionInfo VersionInfo { get; protected set; }

        public string FTPHost { get; protected set; }
        public string FTPBinaryPath { get; protected set; }
        public string FTPUserName { get; protected set; }
        public string FTPPassword { get; protected set; }

        #endregion


        public UploadInstanceToFTPServer(VersionInfo VersionInfo, string ftpHost, string ftpBinaryPath, string ftpUserName, string ftpPassword)
        {
            if (VersionInfo == null) throw new ArgumentNullException("VersionInfo");
            if (string.IsNullOrEmpty(ftpHost)) throw new ArgumentException("Invalid ftpHost", "ftpHost");
            if (string.IsNullOrEmpty(ftpBinaryPath)) throw new ArgumentException("Invalid ftpBinaryPath", "ftpBinaryPath");
            if (string.IsNullOrEmpty(ftpUserName)) throw new ArgumentException("Invalid ftpUserName", "ftpUserName");
            if (string.IsNullOrEmpty(ftpPassword)) throw new ArgumentException("Invalid ftpPassword", "ftpPassword");

            this.VersionInfo = VersionInfo;
            this.FTPHost = ftpHost;
            this.FTPBinaryPath = ftpBinaryPath;
            this.FTPUserName = ftpUserName;
            this.FTPPassword = ftpPassword;

        }


        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Upload started: Upload version to FTP Archive ------");
            Console.ResetColor();


            string folderPath = Path.GetDirectoryName(this.VersionInfo.FileName);

            folderPath = Path.Combine(folderPath, this.VersionInfo.IDTailBase64);

            string[] filesToUpload = Directory.GetFiles(folderPath);


            string ftpDestination = Path.Combine(this.FTPBinaryPath, this.VersionInfo.Version);
            ftpDestination = Path.Combine(ftpDestination, this.VersionInfo.IDTailBase64);


            int filesUploaded = 0;

            Uri host = new Uri(this.FTPHost);
            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);

            foreach (string file in filesToUpload)
            {
                string extention = Path.GetExtension(file);

                if (string.Compare(".exe", extention, true) == 0)   // TODO: Make a filter
                {
                    string ftpFile = Path.Combine(ftpDestination, Path.GetFileName(file));
                    Console.WriteLine("Upload {0} to {1}", file, ftpFile);

                    ftpClient.UploadFile(file, ftpFile);
                    filesUploaded++;

                }

            }

            // Last: Upload VersionInfo_<counter>.xml

            string ftpVersionInfoFile = Path.Combine(this.FTPBinaryPath, this.VersionInfo.Version);

            if (!this.VersionInfo.IsLocked)
            {
                Console.Error.WriteLine("** Warning ** VersionInfo file was not locked, {0}.", this.VersionInfo.FileName);
            }

            string newVersionInfoName = Path.ChangeExtension(this.VersionInfo.FileName, "xml");

            ftpVersionInfoFile = Path.Combine(ftpVersionInfoFile, Path.GetFileName(newVersionInfoName));

            ftpClient.UploadFile(this.VersionInfo.FileName, ftpVersionInfoFile);
            filesUploaded++;



            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Upload succeeded, {0} files uploaded.", filesUploaded);
            Console.ResetColor();

        }



    }
}
