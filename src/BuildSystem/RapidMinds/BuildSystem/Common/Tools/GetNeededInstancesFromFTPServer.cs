using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace RapidMinds.BuildSystem.Common.Tools
{
    public class GetNeededInstancesFromFTPServer
    {
        #region Properties

        public Version Version { get; protected set; }
        public string FTPHost { get; protected set; }
        public string FTPBinaryPath { get; protected set; }
        public string FTPUserName { get; protected set; }
        public string FTPPassword { get; protected set; }

        public int NeededInstances { get; protected set; }


        #endregion


        public GetNeededInstancesFromFTPServer(Version version, string ftpBinaryPath, string ftpHost, string ftpUserName, string ftpPassword)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(ftpHost)) throw new ArgumentException("Invalid ftpHost", "ftpHost");
            if (string.IsNullOrEmpty(ftpBinaryPath)) throw new ArgumentException("Invalid ftpBinaryPath", "ftpBinaryPath");
            if (string.IsNullOrEmpty(ftpUserName)) throw new ArgumentException("Invalid ftpUserName", "ftpUserName");
            if (string.IsNullOrEmpty(ftpPassword)) throw new ArgumentException("Invalid ftpPassword", "ftpPassword");

            this.Version = version;
            this.FTPHost = ftpHost;
            this.FTPBinaryPath = ftpBinaryPath;
            this.FTPUserName = ftpUserName;
            this.FTPPassword = ftpPassword;
        }

        private int _DefaultPoolSize = 3; // PoolSize

        public void Execute()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Get count started: Get needed instances of version {0} ------", this.Version.ToString());
            Console.ResetColor();

            //this.NeededInstances = this.GetNeededCountFromNeededCountFile();
            this.NeededInstances = this.GetNeededCountBasedOnAvailableBinaries();

            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("- Needed count : {0} -", this.NeededInstances);
            Console.ResetColor();

        }

        [Obsolete("Use GetNeededCountBasedOnAvailableBinaries() instead")]
        public int GetNeededCountFromNeededCountFile()
        {


            Uri host = new Uri(this.FTPHost);
            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);


            string path = Path.Combine(this.FTPBinaryPath, this.Version.ToString());
            path = Path.Combine(path, "NeededCount.txt");

            bool bExists = ftpClient.FileExists(path);
            if (bExists == false)
            {
                return this.GetPoolSize();
            }
            else
            {
                string content = ftpClient.ReadFile(path);
                string number = content.Trim();
                return int.Parse(number);
            }

        }

        /// <summary>
        /// Gets the needed count based on available binaries.
        /// </summary>
        /// <returns>Needed instances, -1 if error occured</returns>
        public int GetNeededCountBasedOnAvailableBinaries()
        {
            if (string.IsNullOrEmpty(this.FTPHost))
            {
                return -1;
            }

            Uri host = new Uri(this.FTPHost);
            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);

            string path = Path.Combine(this.FTPBinaryPath, this.Version.ToString());

            try
            {


                if (!ftpClient.DirectoryExists(path))
                {
                    return this.GetPoolSize();
                }

                List<FTPFileInfo> files = ftpClient.GetFiles(path);
                List<VersionInfo> availableInstances = new List<VersionInfo>();

                foreach (FTPFileInfo file in files)
                {

                    if (file.Name.StartsWith("VersionInfo_", StringComparison.CurrentCultureIgnoreCase) == false) continue;
                    if (file.Name.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase) == false) continue;


                    string filepath = Path.Combine(path, file.FullPath.AbsolutePath);

                    try
                    {
                        string content = ftpClient.ReadFile(filepath);

                        XmlSerializer serializer = new XmlSerializer(typeof(VersionInfo));
                        TextReader tr = new StringReader(content);
                        VersionInfo versionInfo = (VersionInfo)serializer.Deserialize(tr);

                        // Check if file <counter>.lock exist
                        string lockFile = Path.Combine(path, string.Format( "{0}.lock", versionInfo.IDTailBase64));
                        if (!ftpClient.FileExists(lockFile))
                        {
                            availableInstances.Add(versionInfo);
                        }
                    }
                    catch (Exception)
                    {
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("** Warning ** Invalid Versioninfo on FTP : {0} -", file.FullPath.AbsolutePath);
                        Console.ResetColor();
                    }

                }


                int poolSize = this.GetPoolSize();

                int needed = poolSize - availableInstances.Count;

                return (needed > 0) ? needed : 0;

            }
            catch (Exception)
            {
                Console.WriteLine("** Warning ** Can not retrive file list from FTP : {0} -", path);
                return -1;
            }



        }


        public int GetPoolSize()
        {


            Uri host = new Uri(this.FTPHost);
            FtpManager ftpClient = new FtpManager(host, this.FTPUserName, this.FTPPassword);

            string path = Path.Combine(this.FTPBinaryPath, this.Version.ToString());
            path = Path.Combine(path, "PoolSize.txt");
            try
            {
                // Check in version folder
                bool bExists = ftpClient.FileExists(path);
                if (bExists == true)
                {
                    string content = ftpClient.ReadFile(path);
                    string number = content.Trim();
                    return int.Parse(number);
                }

                // Check Parent folder (NightlyBuilds/StableBulds/LatestBuilds/PoolSize.txt)
                path = Path.Combine(this.FTPBinaryPath, "PoolSize.txt");
                bExists = ftpClient.FileExists(path);
                if (bExists == true)
                {
                    string content = ftpClient.ReadFile(path);
                    string number = content.Trim();
                    return int.Parse(number);
                }

                // Check Parent folder (NightlyBuilds/StableBulds/PoolSize.txt)
                DirectoryInfo dInfo = new DirectoryInfo(this.FTPBinaryPath);
                string folderName = dInfo.Name; // "NightlyBuilds

                int pos = this.FTPBinaryPath.LastIndexOf(folderName);
                string parentFolder = this.FTPBinaryPath.Substring(0, pos);


                path = Path.Combine(parentFolder, "PoolSize.txt");
                bExists = ftpClient.FileExists(path);
                if (bExists == true)
                {
                    string content = ftpClient.ReadFile(path);
                    string number = content.Trim();
                    return int.Parse(number);
                }



                return this._DefaultPoolSize;
            }
            catch (Exception)
            {
                return this._DefaultPoolSize;
            }

        }


    }
}
