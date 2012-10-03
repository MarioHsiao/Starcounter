using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading;

namespace RapidMinds.BuildSystem.Common
{
    public class FtpManager
    {
        #region Properties

        public string Username { get; protected set; }
        public string Password { get; protected set; }
        public Uri Host { get; protected set; }

        #endregion


        public FtpManager(Uri host, string username, string password)
        {

            if (host == null) throw new ArgumentNullException("host", "Invalid host");
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Invalid username", "username");
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Invalid password", "password");

            this.Host = host;
            this.Username = username;
            this.Password = password;

        }

        // http://www.vcskicks.com/download-file-ftp.php



        public bool CheckServerAccess()
        {
            try
            {

                // The serverUri should start with the ftp:// scheme.
                if (this.Host.Scheme != Uri.UriSchemeFtp)
                {
                    return false;
                }

                //ServicePoint mySP = ServicePointManager.FindServicePoint(this.Host);
                //mySP.ConnectionLimit = 3;
                ////mySP.MaxIdleTime = 0;
                //if (mySP.CurrentConnections > 1)
                //{
                //}
                //mySP.SetTcpKeepAlive(false, 0, 0);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host);

                //ServicePoint mySP = ServicePointManager.FindServicePoint(this.Host);
                //Console.WriteLine("mySP Connecitons:" + mySP.CurrentConnections);
                //Console.WriteLine("Current Connecitons:" + request.ServicePoint.CurrentConnections);

                //mySP.ConnectionLimit = 1;
                //request.ServicePoint.ConnectionLimit = 1;


                //request.KeepAlive = true;
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                //request.Proxy = GlobalProxySelection.GetEmptyWebProxy();
                request.Credentials = new NetworkCredential(this.Username, this.Password);
                //request.UsePassive = true;
                //request.Proxy = null;
                //Console.WriteLine("Passive: {0}  Keep alive: {1}  Binary: {2} Timeout: {3}.",
                //    request.UsePassive,
                //    request.KeepAlive,
                //    request.UseBinary,
                //    request.Timeout == -1 ? "none" : request.Timeout.ToString()
                //);

                //ServicePoint sp = request.ServicePoint;
                //Console.WriteLine("ServicePoint connections = {0}.", sp.ConnectionLimit);
                //sp.ConnectionLimit = 1;

                WebResponse response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        sr.Close();
                    }
                    stream.Close();
                }
                response.Close();
                response = null;

                //GC.Collect();

                return true;
            }
            catch (WebException e)
            {
                throw e;
                //return false;
            }
        }

        public List<FTPDirectoryInfo> GetDirectories(string relativePath)
        {

            Uri uri = new Uri(this.Host, relativePath);


            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            //request.KeepAlive = false;
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(this.Username, this.Password);

            WebResponse response = request.GetResponse();
            List<FTPDirectoryInfo> strList = new List<FTPDirectoryInfo>();


            using (Stream stream = response.GetResponseStream())
            {

                using (StreamReader sr = new StreamReader(stream))
                {


                    while (!sr.EndOfStream)
                    {

                        string line = sr.ReadLine();

                        object item = this.ParseFTPListDetailLine(line);

                        if (item is FTPDirectoryInfo)
                        {
                            string relativeDirectoryPath = Path.Combine(relativePath, ((FTPDirectoryInfo)item).Name);
                            ((FTPDirectoryInfo)item).FullPath = new Uri(this.Host, relativeDirectoryPath);
                            strList.Add((FTPDirectoryInfo)item);
                        }

                    }
                    sr.Close();
                }
                stream.Close();
            }

            response.Close();
            return strList;
        }

        public List<FTPFileInfo> GetFiles(string relativePath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + relativePath);
            //request.KeepAlive = false;
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(this.Username, this.Password);

            try
            {

                List<FTPFileInfo> strList = new List<FTPFileInfo>();
                WebResponse response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {


                        while (!sr.EndOfStream)
                        {

                            string line = sr.ReadLine();

                            object item = this.ParseFTPListDetailLine(line);

                            if (item is FTPFileInfo)
                            {
                                string relativeFilePath = Path.Combine(relativePath, ((FTPFileInfo)item).Name);
                                ((FTPFileInfo)item).FullPath = new Uri(this.Host, relativeFilePath);
                                strList.Add((FTPFileInfo)item);
                            }

                        }

                        sr.Close();
                    }
                    stream.Close();
                }
                response.Close();
                return strList;
            }
            catch (Exception e)
            {
                throw e;
            }


        }

        public List<IBaseInfo> GetList(string relativePath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + relativePath);
            //request.KeepAlive = false;
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(this.Username, this.Password);

            List<IBaseInfo> strList = new List<IBaseInfo>();

            WebResponse response = request.GetResponse();

            using (Stream stream = response.GetResponseStream())
            {

                using (StreamReader sr = new StreamReader(stream))
                {


                    while (!sr.EndOfStream)
                    {

                        string line = sr.ReadLine();

                        object item = this.ParseFTPListDetailLine(line);

                        if (item is FTPFileInfo)
                        {
                            string relativeFilePath = Path.Combine(relativePath, ((FTPFileInfo)item).Name);
                            ((FTPFileInfo)item).FullPath = new Uri(this.Host, relativeFilePath);

                            strList.Add((FTPFileInfo)item);
                        }
                        else if (item is FTPDirectoryInfo)
                        {
                            string relativeDirectoryPath = Path.Combine(relativePath, ((FTPDirectoryInfo)item).Name);
                            ((FTPDirectoryInfo)item).FullPath = new Uri(this.Host, relativeDirectoryPath);

                            strList.Add((FTPDirectoryInfo)item);
                        }

                    }
                    sr.Close();
                }
                stream.Close();
            }

            response.Close();

            return strList;
        }

        private object ParseFTPListDetailLine(string line)
        {


            if (this.IsLinuxStyle(line))
            {
                return this.Parse_Linux_Style(line);
            }
            else if (this.IsIISStyle(line))
            {
                return this.Parse_IIS_Style(line);
                //string name = line.Substring(39);

                //// Parse line
                //if (line.IndexOf("<DIR>", 24, 5) != -1)
                //{
                //    FTPDirectoryInfo dInfo = new FTPDirectoryInfo();
                //    dInfo.Name = name;
                //    return dInfo;
                //}
                //else
                //{
                //    FTPFileInfo fInfo = new FTPFileInfo();
                //    fInfo.Name = name;

                //    long size;
                //    if (long.TryParse(line.Substring(20, 18), out size))   // 20 is not 100%
                //    {
                //        fInfo.Size = size;
                //    }
                //    return fInfo;
                //}
            }
            else
            {
                throw new InvalidProgramException("Can not parse ftp dir listing");
            }

        }

        private bool IsIISStyle(string line)
        {
            return true;
        }

        private IBaseInfo Parse_IIS_Style(string line)
        {
            //"06-23-11  09:18AM       <DIR>          634443403516299602"
            //"06-23-11  09:35AM       <DIR>          a b"
            //"06-22-11  11:52AM               100864 a b c d"
            //"06-23-11  09:34AM       <DIR>          abc"
            //"06-22-11  11:52AM               100864 abcd"

            string name = line.Substring(39);

            // Parse line
            if (line.IndexOf("<DIR>", 24, 5) != -1)
            {
                FTPDirectoryInfo dInfo = new FTPDirectoryInfo();
                dInfo.Name = name;
                return dInfo;
            }
            else
            {
                FTPFileInfo fInfo = new FTPFileInfo();
                fInfo.Name = name;

                long size;
                if (long.TryParse(line.Substring(20, 18), out size))   // 20 is not 100%
                {
                    fInfo.Size = size;
                }
                return fInfo;
            }
        }

        /// <summary>
        /// Determines whether [is linux style] [the specified line].
        /// TODO: Better validation
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>
        ///   <c>true</c> if [is linux style] [the specified line]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsLinuxStyle(string line)
        {
            char type = line[0];

            if (type == 'd') // Directory
            {
                return true;
            }
            else if (type == '-') // File
            {
                return true;
            }
            else if (type == 'l') // Link
            {
                return true;
            }

            return false;
        }

        private IBaseInfo Parse_Linux_Style(string line)
        {
            // http://www.zzee.com/solutions/linux-permissions.shtml

            //"drwx--x--x   10 rapid      rapid            4096 Jun  9 01:02 ."
            //"drwx--x--x   10 rapid      rapid            4096 Jun  9 01:02 .."
            //"-rw-r--r--    1 rapid      rapid              33 Jun  8 14:19 .bash_logout"
            //"-rw-r--r--    1 rapid      rapid             176 Jun  8 14:19 .bash_profile"
            //"-rw-r--r--    1 rapid      rapid             124 Jun  8 14:19 .bashrc"
            //"-rw-------    1 rapid      rapid               0 Jun  8 14:19 .contactemail"
            //"drwx------    5 rapid      rapid            4096 Jun  9 12:20 .cpanel"
            //"-rw-r--r--    1 rapid      rapid             123 Jun  8 14:19 .gemrc"
            //"drwxr-x---    2 rapid      99               4096 Jun  8 14:19 .htpasswds"
            //"-rw-------    1 rapid      rapid              10 Jun  8 17:02 .lastlogin"
            //"drwx------    2 rapid      rapid            4096 Jun  8 17:02 .trash"
            //"lrwxrwxrwx    1 rapid      rapid              31 Jun  8 14:27 access-logs -> /usr/local/apache/domlogs/rapid"
            //"-rw-r-----    1 rapid      rapid               1 Jun  9 01:02 cpbackup-exclude.conf"
            //"drwxr-x---    2 rapid      12               4096 Jun  8 14:19 etc"
            //"drwxr-x---    8 rapid      rapid            4096 Jun  8 14:19 mail"
            //"drwxr-xr-x    3 rapid      rapid            4096 Jun  8 14:19 public_ftp"
            //"drwxr-x---   15 rapid      99               4096 Jun 16 12:16 public_html"
            //"drwxr-xr-x    7 rapid      rapid            4096 Jun  9 12:20 tmp"
            //"lrwxrwxrwx    1 rapid      rapid              11 Jun  8 14:19 www -> public_html"


            char type = line[0];

            if (type == 'd') // Directory
            {
                FTPDirectoryInfo dInfo = new FTPDirectoryInfo();
                dInfo.Name = line.Substring(62);
                if (dInfo.Name.Equals(".") || dInfo.Name.Equals(".."))
                {
                    return null;
                }
                return dInfo;

            }
            else if (type == '-') // File
            {
                FTPFileInfo fInfo = new FTPFileInfo();
                fInfo.Name = line.Substring(62);
                return fInfo;
            }
            else if (type == 'l') // Link
            {
                FTPLinkInfo lInfo = new FTPLinkInfo();
                int pos = line.IndexOf(" -> ");
                if (pos != -1)
                {
                    lInfo.Name = line.Substring(62, pos - 62);
                    return lInfo;
                }
            }

            return null;
        }

        //public long GetFileSize(Uri uri)
        //{
        //    FtpWebRequest request = FtpWebRequest.Create(uri) as FtpWebRequest;
        //    request.Method = WebRequestMethods.Ftp.GetFileSize;
        //    request.Credentials = new NetworkCredential("anonymous", "janeDoe@contoso.com");

        //    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        //    {
        //        return response.ContentLength;
        //    }


        //}

        //public void Download(string file, string destination)
        //{
        //    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + file);
        //    request.Method = WebRequestMethods.Ftp.DownloadFile;
        //    request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
        //    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        //    Stream responseStream = response.GetResponseStream();
        //    StreamReader reader = new StreamReader(responseStream);

        //    StreamWriter writer = new StreamWriter(destination);
        //    writer.Write(reader.ReadToEnd());

        //    writer.Close();
        //    reader.Close();
        //    response.Close();
        //}

        //public List<string> DirectoryListing()
        //{
        //    return DirectoryListing(string.Empty);
        //}

        //public List<string> DirectoryListing(string folder)
        //{
        //    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + folder);
        //    request.Method = WebRequestMethods.Ftp.ListDirectory;
        //    request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
        //    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        //    Stream responseStream = response.GetResponseStream();
        //    StreamReader reader = new StreamReader(responseStream);

        //    List<string> result = new List<string>();

        //    while (!reader.EndOfStream)
        //    {
        //        result.Add(reader.ReadLine());
        //    }

        //    reader.Close();
        //    response.Close();
        //    return result;
        //}

        public void UploadFile(string source, string relativeFilePath)
        {

            string directoryPath = Path.GetDirectoryName(relativeFilePath);

            if (!this.DirectoryExists(directoryPath))
            {
                this.CreateDirectory(directoryPath);
            }


            Uri destinationUri = new Uri(this.Host, relativeFilePath);

            FileInfo fileInf = new FileInfo(source);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(destinationUri);
            //request.KeepAlive = false;
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(this.Username, this.Password);

            // By default KeepAlive is true, where the control connection is not closed
            // after a command is executed.
            //request.KeepAlive = false;


            // Specify the data transfer type.
            request.UseBinary = true;

            // Notify the server about the size of the uploaded file
            request.ContentLength = fileInf.Length; // fileContents.Length;

            // The buffer size is set to 2kb
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;

            try
            {

                using (FileStream fs = fileInf.OpenRead())
                {

                    // Stream to which the file to be upload is written
                    using (Stream strm = request.GetRequestStream())
                    {
                        // Read from the file stream 2kb at a time
                        contentLen = fs.Read(buff, 0, buffLength);

                        // Till Stream content ends
                        while (contentLen != 0)
                        {
                            // Write Content from the file stream to the FTP Upload Stream
                            strm.Write(buff, 0, contentLen);
                            contentLen = fs.Read(buff, 0, buffLength);
                        }

                        // Close the file stream and the Request Stream
                        strm.Close();
                    }
                    fs.Close();
                }

            }
            catch (Exception e)
            {

                throw e;
                //MessageBox.Show(ex.Message, "Upload Error");
            }


        }

        public void CreateDirectory(string path)
        {
            try
            {
                string[] paths = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string finalPath = string.Empty;
                foreach (string directory in paths)
                {
                    finalPath += "/" + directory;

                    if (!this.DirectoryExists(finalPath))
                    {

                        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + finalPath);
                        //request.KeepAlive = false;
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = new NetworkCredential(this.Username, this.Password);

                        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                        response.Close();
                    }

                }


            }
            catch (WebException e)
            {
                throw e;
                //FtpWebResponse response = (FtpWebResponse)ex.Response;
                //if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                //{
                //}
                //else
                //{
                //}
            }
        }

        public bool DirectoryExists(string path)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + "/" + path+"/");
                //request.KeepAlive = false;

                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(this.Username, this.Password);

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (WebException e)
            {
                FtpWebResponse response = (FtpWebResponse)e.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
                else
                {
                    throw e;
                }
            }
        }

        public bool FileExists(string path)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + "/" + path);
                //request.KeepAlive = false;

                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                request.Credentials = new NetworkCredential(this.Username, this.Password);

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (WebException e)
            {
                FtpWebResponse response = (FtpWebResponse)e.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
                else
                {
                    throw e;
                }
            }
        }

        public DateTime GetTimeStamp(string relativePath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + "/" + relativePath);
                //request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                request.Credentials = new NetworkCredential(this.Username, this.Password);

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                DateTime timeStamp = response.LastModified;

                response.Close();

                return timeStamp;

            }
            catch (WebException e)
            {
                throw e;
            }
        }

        public string ReadFile(string path)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.Host + "/" + path);
            //request.KeepAlive = false;
            request.Credentials = new NetworkCredential(this.Username, this.Password);

            //GET THE FTP RESPONSE
            using (WebResponse response = request.GetResponse())
            {
                //GET THE STREAM TO READ THE RESPONSE FROM
                using (Stream tmpStream = response.GetResponseStream())
                {
                    //CREATE A TXT READER (COULD BE BINARY OR ANY OTHER TYPE YOU NEED)
                    using (TextReader tmpReader = new StreamReader(tmpStream))
                    {
                        //STORE THE FILE CONTENTS INTO A STRING
                        return tmpReader.ReadToEnd();
                    }
                }
            }

        }

    }


    public interface IBaseInfo
    {
        string Name { get; set; }
        Uri FullPath { get; set; }
    }

    public class FTPDirectoryInfo : IBaseInfo
    {
        public string Name { get; set; }
        public Uri FullPath { get; set; }
    }

    public class FTPFileInfo : IBaseInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public Uri FullPath { get; set; }
    }

    public class FTPLinkInfo : IBaseInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public Uri FullPath { get; set; }
    }
}
