using System;
using Codeplex.Data;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using Starcounter.Internal;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Xml;

namespace Starcounter.Administrator.Server.Utilities {
    /// <summary>
    /// General utils
    /// </summary>
    internal class RestUtils {

        /// <summary>
        /// Create an Error Json response from an exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Response</returns>
        public static Response CreateErrorResponse(Exception e) {

            ErrorResponse errorResponse = new ErrorResponse();
            errorResponse.Text = e.Message;
            errorResponse.StackTrace = e.StackTrace;
            errorResponse.Helplink = e.HelpLink;

            return new Response() { BodyBytes = errorResponse.ToJsonUtf8(), StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError };
        }

        /// <summary>
        /// Create Database settings
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static DatabaseSettings CreateSettings(DatabaseInfo database) {

            DatabaseSettings settings = new DatabaseSettings();

            settings.Name = database.Name;
            settings.DefaultUserHttpPort = database.Configuration.Runtime.DefaultUserHttpPort;
            settings.SchedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount;
            settings.ChunksNumber = database.Configuration.Runtime.ChunksNumber;

            // TODO: this is a workaround to get the default dumpdirectory path (fix this in the public model api)
            if (string.IsNullOrEmpty(database.Configuration.Runtime.DumpDirectory)) {
                //  By default, dump files are stored in ImageDirectory
                settings.DumpDirectory = database.Configuration.Runtime.ImageDirectory;
            }
            else {
                settings.DumpDirectory = database.Configuration.Runtime.DumpDirectory;
            }

            settings.TempDirectory = database.Configuration.Runtime.TempDirectory;
            settings.ImageDirectory = database.Configuration.Runtime.ImageDirectory;
            settings.TransactionLogDirectory = database.Configuration.Runtime.TransactionLogDirectory;
            settings.CollationFile = database.CollationFile;
            settings.FirstObjectID = (long) database.FirstObjectID;
            settings.LastObjectID = (long) database.LastObjectID;
            return settings;
        }

        /// <summary>
        /// Create Server settings
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static ServerSettings CreateSettings(ServerInfo server) {

            ServerSettings settings = new ServerSettings();

            settings.Name = server.Configuration.Name;
            settings.SystemHttpPort = server.Configuration.SystemHttpPort;

            string gwConfig = Path.Combine(server.Configuration.EnginesDirectory, StarcounterEnvironment.FileNames.GatewayConfigFileName);

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(File.ReadAllText(gwConfig)); // suppose that myXmlString contains "<Names>...</Names>"

            string aPort = xml.SelectSingleNode("/NetworkGateway/" + MixedCodeConstants.GatewayAggregationPortSettingName).InnerText;
            long aggregationPort = 0;

            long.TryParse(aPort, out aggregationPort);

            settings.AggregationPort = aggregationPort;

            settings.Version = CurrentVersion.Version;
            settings.VersionDate = CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            settings.Edition = CurrentVersion.EditionName;
            settings.Channel = CurrentVersion.ChannelName;

            return settings;
        }


        /// <summary>
        /// Validate Database settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>ValidationErrors</returns>
        public static ValidationErrors GetValidationErrors(DatabaseSettings settings) {

            ValidationErrors validationErrors = new ValidationErrors();

            // Database name
            if (string.IsNullOrEmpty(settings.Name)) {

                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "Name";
                validationError.Text = "invalid database name";
            }

            // Port number
            if (settings.DefaultUserHttpPort < IPEndPoint.MinPort || settings.DefaultUserHttpPort > IPEndPoint.MaxPort) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "DefaultUserHttpPort";
                validationError.Text = "invalid port number";
            }

            // Scheduler Count
            if (settings.SchedulerCount < 0) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "SchedulerCount";
                validationError.Text = "invalid scheduler count";
            }

            // Chunks Number
            if (settings.ChunksNumber < 0) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "ChunksNumber";
                validationError.Text = "invalid chunks number";
            }

            // Dump Directory
            if (string.IsNullOrEmpty(settings.DumpDirectory)) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "DumpDirectory";
                validationError.Text = "invalid dump directory";
            }

            // Temp Directory
            if (string.IsNullOrEmpty(settings.TempDirectory)) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "TempDirectory";
                validationError.Text = "invalid temp directory";
            }

            // Image Directory
            if (string.IsNullOrEmpty(settings.ImageDirectory)) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "ImageDirectory";
                validationError.Text = "invalid image directory";
            }

            // Log Directory
            if (string.IsNullOrEmpty(settings.TransactionLogDirectory)) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "TransactionLogDirectory";
                validationError.Text = "invalid transaction log directory";
            }

            // TODO: Validate the collation file
            // Collation File
            //if (string.IsNullOrEmpty(settings.CollationFile)) {
            //    var validationError = validationErrors.Items.Add();
            //    validationError.PropertyName = "CollationFile";
            //    validationError.Text = "invalid collation file";
            //}

            return validationErrors;
        }


        /// <summary>
        /// Validate Server settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>ValidationErrors</returns>
        public static ValidationErrors GetValidationErrors(ServerSettings settings) {

            ValidationErrors validationErrors = new ValidationErrors();

            // Port number
            if (settings.SystemHttpPort < IPEndPoint.MinPort || settings.SystemHttpPort > IPEndPoint.MaxPort) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "SystemHttpPort";
                validationError.Text = "invalid port number";
            }

            if (settings.AggregationPort < IPEndPoint.MinPort || settings.AggregationPort > IPEndPoint.MaxPort) {
                var validationError = validationErrors.Items.Add();
                validationError.PropertyName = "AggregationPort";
                validationError.Text = "invalid port number";
            }

            return validationErrors;
        }

        /// <summary>
        /// TODO: 
        /// </summary>
        /// <returns></returns>
        public static string GetMachineIp() {

//            string url = new Uri(Starcounter.Administrator.API.Handlers.RootHandler.Host.BaseUri, relative).ToString();

            IPHostEntry host;
            string localIP = "127.0.0.1";
            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        public static byte[] GetHash(string inputString) {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString) {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }


    }
}
