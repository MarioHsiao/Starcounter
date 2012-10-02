using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using RapidMinds.BuildSystem.Common.Tools;

namespace RapidMinds.BuildSystem.Common.Tasks
{

//    public class FillBinariesToFTPArchiveTask
//    {
//        #region Properties

//        //public Configuration Configuration { get; protected set; }

//        public string SourceArchive { get; protected set; }
//        public string BinaryArchive { get; protected set; }

//        public string FTPHost { get; protected set; }
//        public string FTPBinaryPath { get; protected set; }
//        public string FTPUserName { get; protected set; }
//        public string FTPPassword { get; protected set; }

//        public IBuilder Builder { get; protected set; }

//        #endregion


//        public FillBinariesToFTPArchiveTask(IBuilder builder, string binaryArchive, string sourceArchive, string ftpHost, string ftpBinaryPath, string ftpUserName, string ftpPassword)
//        {
//            if (builder == null) throw new ArgumentNullException("builder");
//            if (string.IsNullOrEmpty(binaryArchive)) throw new ArgumentException("Invalid binaryArchive", "binaryArchive");
//            if (string.IsNullOrEmpty(sourceArchive)) throw new ArgumentException("Invalid sourceArchive", "sourceArchive");
//            if (string.IsNullOrEmpty(ftpHost)) throw new ArgumentException("Invalid ftpHost", "ftpHost");
//            if (string.IsNullOrEmpty(ftpBinaryPath)) throw new ArgumentException("Invalid ftpBinaryPath", "ftpBinaryPath");
//            if (string.IsNullOrEmpty(ftpUserName)) throw new ArgumentException("Invalid ftpUserName", "ftpUserName");
//            if (string.IsNullOrEmpty(ftpPassword)) throw new ArgumentException("Invalid ftpPassword", "ftpPassword");

//            this.Builder = builder;
//            this.BinaryArchive = binaryArchive;
//            this.SourceArchive = sourceArchive;
//            this.FTPHost = ftpHost;
//            this.FTPBinaryPath = ftpBinaryPath;
//            this.FTPUserName = ftpUserName;
//            this.FTPPassword = ftpPassword;

//        }


//        public void Execute()
//        {
//            Console.ResetColor();
//            Console.ForegroundColor = ConsoleColor.Magenta;
//            Console.WriteLine();
//            Console.WriteLine("== <<TODO: Copy binaries to FTP archive TODO>> ==");
//            Console.WriteLine();
//            Console.ResetColor();


//            // Check FTP Server Access
//            this.CheckServerAccess();


//            // Get available versions in Source archive
//            List<Version> versions = this.GetAvailableVersionsInSourceArchive();

//            // Get available binary versions in FTP Archive
//            foreach (Version version in versions)
//            {

//                GetNeededInstancesFromFTPServer task = new GetNeededInstancesFromFTPServer(version,
//                    this.FTPBinaryPath,
//                    this.FTPHost,
//                    this.FTPUserName,
//                    this.FTPPassword);

//                task.Execute();

//                int num = task.NeededInstances;

//                if (num > 0)
//                {
//                    this.UploadInstances(version, num);
//                }

//            }


//        }

//        private void CheckServerAccess()
//        {

//            Uri host = new Uri(this.FTPHost);
//            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);
//            if (!ftpClient.CheckServerAccess())
//            {
//                throw new InvalidOperationException(string.Format("Can not access FTP server {0}.", this.FTPHost));
//            }
//        }


//        /// <summary>
//        /// Gets the available sources in Source Archive.
//        /// </summary>
//        /// <remarks>
//        /// Returns a list of vaild version folder names in Source archive.
//        /// </remarks>
//        /// <returns>Returns a list of vaild version folder names in Source archive.</returns>
//        private List<Version> GetAvailableVersionsInSourceArchive()
//        {
//            List<Version> versions = new List<Version>();

//            string[] directories = Directory.GetDirectories(this.SourceArchive);

//            foreach (string directory in directories)
//            {

//                try
//                {
//                    // Check if valid directory

//                    string[] folders = directory.Split(new char[2] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

//                    if (folders == null || folders.Length == 0) continue;

//                    // Get Last folder
//                    string folderName = folders[folders.Length - 1];
//                    Version version = new Version(folderName);

//                    // TODO: Maybe also check for valid content?

//                    versions.Add(version);
//                }
//                catch (Exception)
//                {
//                    // Ignore not valid folders
//                }
//            }
//            return versions;
//        }

//        /// <summary>
//        /// Gets the available binaries on FTP server.
//        /// TODO: Do not include "locked" instances (VerionInfo_[counter].lock
//        /// </summary>
//        /// <param name="version">The version.</param>
//        /// <returns></returns>
//        //private int GetAvailableBinariesOnFtpServer(Version version)
//        //{
//        //    // c:\ftp\files\builds\nightlybuilds\1.0.30.0\634443403516299602\SpeedGrid_1.0.30.0.exe

//        //    Uri host = new Uri(this.FTPHost);
//        //    FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);

//        //    string ftpPath = Path.Combine(this.FTPBinaryPath, version.ToString());

//        //    int numberOfInstances = 0;

//        //    try
//        //    {

//        //        List<FTPDirectoryInfo> directories = ftpClient.GetDirectories(ftpPath);

//        //        foreach (FTPDirectoryInfo directory in directories)
//        //        {
//        //            // Check if directory is a valid version directory

//        //            // Check folder content
//        //            List<FTPFileInfo> files = ftpClient.GetFiles(directory.FullPath.AbsolutePath);
//        //            bool bValidFolder = false;
//        //            foreach (FTPFileInfo file in files)
//        //            {
//        //                // TODO: Better check
//        //                if (file.Name.IndexOf("speedgrid", StringComparison.CurrentCultureIgnoreCase) != -1)
//        //                {
//        //                    // Ok
//        //                    bValidFolder = true;
//        //                    break;
//        //                }
//        //            }

//        //            if (bValidFolder == false) continue;

//        //            numberOfInstances++;

//        //            //Console.WriteLine("Valid Directory: {0}", directory.FullPath);
//        //        }

//        //    }
//        //    catch (WebException e)
//        //    {
//        //        if (e.Status == WebExceptionStatus.ProtocolError) // Wrong username and password
//        //        {
//        //        }
//        //    }
//        //    catch (Exception e)
//        //    {
//        //    }

//        //    //List<FTPFileInfo> files = ftpClient.GetFiles(ftpPath);
//        //    //foreach (FTPFileInfo file in files)
//        //    //{
//        //    //    Console.WriteLine("File: {0} ({1} bytes)", file.FullPath, file.Size);
//        //    //}

//        //    //Console.WriteLine("--GetList--");

//        //    //List<IBaseInfo> items = ftpClient.GetList(ftpPath);

//        //    //foreach (IBaseInfo item in items)
//        //    //{

//        //    //    if (item is FTPDirectoryInfo)
//        //    //    {
//        //    //        Console.WriteLine("Directory: {0}", item.FullPath);
//        //    //    }
//        //    //    else if (item is FTPFileInfo)
//        //    //    {
//        //    //        Console.WriteLine("File: {0} ({1} bytes)", item.FullPath, ((FTPFileInfo)item).Size);
//        //    //    }

//        //    //}


//        //    return numberOfInstances;
//        //}



//        private void UploadInstances(Version version, int numberToUpload)
//        {
//            //Console.WriteLine("TODO: Build {0} of version {1}", numberToUpload, version.ToString());


//            for (int i = 0; i < numberToUpload; i++)
//            {
//                VersionInfo versionInfo = this.GetInstance(version);

//                if (versionInfo == null)
//                {
//                    // We need to build some more instances
//                    versionInfo = this.Build(version);
//                }

//                if (versionInfo != null)
//                {
//                    this.UploadInstance(versionInfo);
//                }
//            }

//        }


//        private VersionInfo Build(Version version)
//        {
//            Console.WriteLine("TODO: Build");

////            this.Builder.Build(


//            return null;
//        }

//        private void UploadInstance(VersionInfo versionInfo)
//        {

//            UploadInstanceToFTPServer task = new UploadInstanceToFTPServer(versionInfo,
//                this.FTPHost,
//                this.FTPBinaryPath,
//                this.FTPUserName,
//                this.FTPPassword);

//            task.Execute();

//        }



//        /// <summary>
//        /// Gets version instance from Binary Archive
//        /// </summary>
//        /// <remarks>
//        /// The VersionFile will be locked (*.lock)
//        /// </remarks>
//        /// <param name="version">The version.</param>
//        /// <returns></returns>
//        private VersionInfo GetInstance(Version version)
//        {

//            string binPath = Path.Combine(this.BinaryArchive, version.ToString());

//            string[] files = Directory.GetFiles(binPath, "VersionInfo_*.xml");


//            foreach (string file in files)
//            {

//                VersionInfo versionInfo = VersionInfo.Load(file);

//                // Check for "real" instance
//                string instanceDirectory = Path.Combine(binPath, versionInfo.IDTailBase64);

//                if (Directory.Exists(instanceDirectory))
//                {
//                    VersionInfo.LockInstance(versionInfo);
//                    return versionInfo;
//                }
//                else
//                {
//                    Console.WriteLine(string.Format("** Warning ** Incorrect folder structure, VersionInfo points to a non existing instance folder {0}", instanceDirectory));
//                }

//            }


//            return null;
//        }


//    }

}
