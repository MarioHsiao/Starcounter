using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class UploadClassAPItoFTPServer
    {

        // Command: c:\FTP\SCDev\BuildSystem\InstallshieldMod.exe "%teamcity.build.checkoutDir%\Setup Installer\Setup Installer.isl" "%env.BUILD_NUMBER%" "%env.RAMDISK%\"

        public Version Version { get; protected set; }
        public string BinaryArchive { get; protected set; }
        public string FTPDestination { get; protected set; }
        public string FTPHost { get; protected set; }
        public string FTPUserName { get; protected set; }
        public string FTPPassword { get; protected set; }

        public UploadClassAPItoFTPServer(Version version, string binaryArchive, string ftpDestination, string ftpHost, string ftpUserName, string ftpPassword)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(binaryArchive)) throw new ArgumentException("Invalid binaryArchive", "binaryArchive");

            if (string.IsNullOrEmpty(ftpDestination)) throw new ArgumentException("Invalid ftpDestination", "ftpDestination");
            if (string.IsNullOrEmpty(ftpHost)) throw new ArgumentException("Invalid ftpHost", "ftpHost");
            if (string.IsNullOrEmpty(ftpUserName)) throw new ArgumentException("Invalid ftpUserName", "ftpUserName");
            //if (string.IsNullOrEmpty(ftpPassword)) throw new ArgumentException("Invalid ftpPassword", "ftpPassword");

            this.Version = version;
            this.BinaryArchive = binaryArchive;
            this.FTPDestination = ftpDestination;
            this.FTPHost = ftpHost;
            this.FTPUserName = ftpUserName;
            this.FTPPassword = ftpPassword;
        }

        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Upload started: Uploading Class API Documentation ------");
            Console.ResetColor();


            Uri host = new Uri(this.FTPHost);
            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);

            int filesCopied = 0;
            this.UploadFolder( ftpClient, this.BinaryArchive, this.FTPDestination, ref filesCopied );


            // Create "Done file".
            string doneFile = Path.Combine(this.BinaryArchive, "uploaded.ok");
            TextWriter tx = new StreamWriter(doneFile);
            tx.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:dd}", DateTime.Now));
            tx.Close();

            // Upload "Done file".
            string destinationDoneFile = Path.Combine( this.FTPDestination, "uploaded.ok" );
            ftpClient.UploadFile(doneFile, destinationDoneFile);


            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Upload succeeded, {0} files uploaded.", filesCopied);
            Console.ResetColor();

        }



        /// <summary>
        /// Copy directory structure recursively
        /// </summary>
        /// <param name="sourceFolder">The source folder.</param>
        /// <param name="destinationFolder">The destination folder.</param>
        /// <param name="filesCopied">The files copied.</param>
        public void UploadFolder(FtpManager ftpClient, string sourceFolder, string destinationFolder, ref int filesCopied)
        {
            String[] files;

            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException(string.Format("Can not find Class Documentation API folder, {0}", sourceFolder));
            }

            if (!ftpClient.DirectoryExists(destinationFolder))
            {
                ftpClient.CreateDirectory(destinationFolder);
            }

            files = Directory.GetFileSystemEntries(sourceFolder);
            foreach (string element in files)
            {
                // Sub directories

                if (Directory.Exists(element))
                {
                    UploadFolder(ftpClient, element, Path.Combine( destinationFolder ,Path.GetFileName(element)), ref filesCopied);
                }
                else
                {
                    // TODO: check if file exist and is "current".


                    ftpClient.UploadFile(element, Path.Combine(destinationFolder, Path.GetFileName(element)));
                    filesCopied++;
                }
            }

        }




    }

}
