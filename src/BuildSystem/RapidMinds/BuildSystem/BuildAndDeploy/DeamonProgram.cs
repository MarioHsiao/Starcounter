using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using RapidMinds.BuildSystem.Common;
using RapidMinds.BuildSystem.Common.Tasks;
using RapidMinds.BuildSystem.Common.Tools;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;

namespace RapidMinds.BuildSystem.BuildAndDeploy
{
    public class DeamonProgram
    {

        #region Properties

        private DeamonConfiguration DeamonConfiguration { get; set; }

        private String WorkingDirectory { get; set; }
        private String ExeDirectory { get; set; }

        #endregion

        #region Ctrl+C
        [DllImport("Kernel32")]

        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {

            DeamonProgram.WriteToLog(string.Format("Deamon Process killed ({0})", ctrlType));
            Console.WriteLine("[{0}] Deamon Process killed ({1})", DateTime.Now.ToString(), ctrlType);

            return false;

        }
        void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DeamonProgram.WriteToLog(string.Format("Deamon Process CancelKeyPress"));
            Console.WriteLine("[{0}] Deamon CancelKeyPress", DateTime.Now.ToString());

        }
        #endregion

        public void Start()
        {

            this.WorkingDirectory = Directory.GetCurrentDirectory();
            this.ExeDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {

                if (this.CanStart())
                {
                    DeamonProgram.WriteToLog("Deamon Started");
                    Console.WriteLine("[{0}] Deamon Started", DateTime.Now.ToString());
                    Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
                    HandlerRoutine hr = new HandlerRoutine(ConsoleCtrlCheck);
                    GC.KeepAlive(hr);
                    SetConsoleCtrlHandler(hr, true);

                    this.AddFileWatcher();

                    this.DeamonConfiguration = this.Load(Path.Combine(this.ExeDirectory, DeamonProgram.ConfigFile));

                    this.StartSequenceLoop();

                    DeamonProgram.WriteToLog("Deamon Ended");
                    Console.WriteLine("[{0}] Deamon Ended", DateTime.Now.ToString());
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[{0}] Deamon already running.", DateTime.Now.ToString());
                    Console.ResetColor();
                }

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                DeamonProgram.WriteToLog(e.Message);
                Console.WriteLine("[{0}] {1}", DateTime.Now.ToString(), e.Message);
                Console.ResetColor();

                DeamonProgram.WriteToLog("Deamon Ended");
                Console.WriteLine("[{0}] Deamon Ended", DateTime.Now.ToString());

                return;
            }
            finally
            {
                this.RemoveFileWatcher();
                if (this._mutex != null)
                {
                    this._mutex.ReleaseMutex();
                }
            }
        }

        Mutex _mutex;
        private bool CanStart()
        {
            string mutexName = "BuildAndDeploy";

            this._mutex = new Mutex(false, @"Global\" + mutexName);

            if (!this._mutex.WaitOne(0, false))
            {
                this._mutex = null;
                return false;   // Instance already running
            }

            this._mutex = new Mutex(false, @"Local\" + mutexName);
            if (!this._mutex.WaitOne(0, false))
            {
                this._mutex = null;
                return false;   // Instance already running
            }


            return true;

        }

        #region File Watcher

        FileSystemWatcher FileWatcher;

        /// <summary>
        /// Adds the file watcher.
        /// </summary>
        /// <param name="file">The file.</param>
        private void AddFileWatcher()
        {
            //string file = Path.Combine(Directory.GetCurrentDirectory(), "deamon.exit");
            //string file = Directory.GetCurrentDirectory();
            this.FileWatcher = new FileSystemWatcher();

            //string path = System.IO.Path.GetDirectoryName(file);

            this.FileWatcher.Path = this.ExeDirectory;
            //this.FileWatcher.Filter = "deamon.*";
            //this.FileWatcher.Filter = System.IO.Path.GetFileName(file);
            this.FileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            // Add event handlers.
            this.FileWatcher.Changed += new FileSystemEventHandler(OnChanged);
            this.FileWatcher.Created += new FileSystemEventHandler(OnChanged);
            //this.FileWatcher.Deleted += new FileSystemEventHandler(FileWatcher_Deleted);
            this.FileWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            FileWatcher.EnableRaisingEvents = true;
        }


        private void RemoveFileWatcher()
        {
            if (this.FileWatcher != null)
            {
                this.FileWatcher.Dispose();
            }
        }


        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = source as FileSystemWatcher;
            watcher.EnableRaisingEvents = false;    // Bug workaround, Microsoft sends double events
            watcher.EnableRaisingEvents = true;     // Bug workaround, Microsoft sends double events

            this.CheckForFlagFiles();


        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            this.CheckForFlagFiles();
        }

        private void CheckForFlagFiles()
        {
        recheck:

            // Stop Sequence
            if (File.Exists(Path.Combine(this.ExeDirectory, "deamon.exit")))
            {
                try
                {
                    //// Remove readonly attribute
                    FileInfo info = new FileInfo((Path.Combine(this.ExeDirectory, "deamon.exit")));
                    info.Attributes &= ~FileAttributes.ReadOnly;

                    File.Delete(Path.Combine(this.ExeDirectory, "deamon.exit"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("[{0}] ** Warning ** deamon.exit - {1}", DateTime.Now.ToString(), e.Message);
                    Thread.Sleep(100);
                    goto recheck;
                }
                this.bExit = true;
                this.waitHandle.Set();
                return;
            }

            // Trigger sequence
            if (File.Exists(Path.Combine(this.ExeDirectory, "deamon.trigger")))
            {

                try
                {
                    //// Remove readonly attribute
                    FileInfo info = new FileInfo(Path.Combine(this.ExeDirectory, "deamon.trigger"));
                    info.Attributes &= ~FileAttributes.ReadOnly;

                    File.Delete(Path.Combine(this.ExeDirectory, "deamon.trigger"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("[{0}] ** Warning ** deamon.trigger - {1}", DateTime.Now.ToString(), e.Message);
                    Thread.Sleep(100);
                    goto recheck;
                }
                this.waitHandle.Set();
                return;
            }
        }


        static public void Trigger()
        {
            try
            {
                string ExeDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                TextWriter textWriter = new StreamWriter(Path.Combine(ExeDirectory, "deamon.trigger"));
                textWriter.WriteLine("bump");
                textWriter.Close();
                textWriter.Dispose();
            }
            catch (Exception)
            {
            }
        }



        #endregion

        bool bExit = false;
        AutoResetEvent waitHandle = new AutoResetEvent(true);
        int dynamicLoopDelay = 1000 * 60 * 1; // 1 min
        private const string ConfigFile = "Deamon.config";

        #region Load and Save configuration

        public void Configuration_Example()
        {


            DeamonConfiguration configurations = this.Load(Path.Combine(this.ExeDirectory, DeamonProgram.ConfigFile));

            if (configurations == null)
            {
                configurations = this.CreateTestConfigurations();
                this.Save(configurations, Path.Combine(this.ExeDirectory, DeamonProgram.ConfigFile));
            }

        }

        public DeamonConfiguration CreateTestConfigurations()
        {

            DeamonConfiguration config = new DeamonConfiguration();

            config.Frequence = 10;
            config.IsEnabled = true;

            config.Projects = new List<Project>();

            Project config1 = new Project();
            config1.Name = "Config 1";
            config1.Path = Directory.GetCurrentDirectory();
            config1.IsEnabled = true;

            config.Projects.Add(config1);

            Project config2 = new Project();
            config2.Name = "Config 2";
            config2.Path = Directory.GetCurrentDirectory();
            config2.IsEnabled = false;

            config.Projects.Add(config2);

            return config;
        }

        private void Save(DeamonConfiguration config, string file)
        {
            try
            {

                // Serialize 
                XmlSerializer serializer = new XmlSerializer(typeof(DeamonConfiguration));
                FileStream fs = new FileStream(file, FileMode.Create);
                serializer.Serialize(fs, config);
                fs.Close();
            }
            catch (Exception e)
            {
            }
        }

        private DeamonConfiguration Load(string file)
        {
            try
            {
                // Deserialize 
                FileStream fs = new FileStream(file, FileMode.Open);
                XmlSerializer serializer = new XmlSerializer(typeof(DeamonConfiguration));
                DeamonConfiguration config = (DeamonConfiguration)serializer.Deserialize(fs);
                return config;
            }
            catch (FileNotFoundException e)
            {
                // Create default
                DeamonConfiguration config = new DeamonConfiguration();
                config.Frequence = 10;
                config.IsEnabled = true;

                config.Projects = new List<Project>();

                Project project = new Project();
                project.Name = "Test Project Name";
                project.Path = Directory.GetCurrentDirectory();
                config.Projects.Add(project);
                this.Save(config, Path.Combine(this.ExeDirectory, DeamonProgram.ConfigFile));
                return config;
            }
        }


        #endregion


        private void StartSequenceLoop()
        {

            this.CheckForFlagFiles();

            while (true)
            {

                if (bExit == true)
                {
                    break;
                }

                waitHandle.WaitOne(dynamicLoopDelay);  // 1 minutes

                if (bExit == true)
                {
                    break;
                }


                foreach (Project project in this.DeamonConfiguration.Projects)
                {


                    string projectPath;

                    if (Path.IsPathRooted(project.Path) == true)
                    {
                        // absolute

                        projectPath = project.Path;
                    }
                    else
                    {
                        // relative
                        projectPath = Path.Combine(this.ExeDirectory, project.Path);
                    }

                    // Load project configuration files
                    //string[] files = Directory.GetFiles(".", "*.config");
                    string[] files = Directory.GetFiles(projectPath, "*.config");

                    foreach (string configFile in files)
                    {

                        Configuration configuration = Configuration.Load(configFile);


                        if (string.IsNullOrEmpty(configuration.Plugin))
                        {
                            throw new FileNotFoundException("Missing plugin in configuration");
                        }

                        string projectPlugin = Path.Combine(projectPath, configuration.Plugin);

                        try
                        {

                            // Load plugin
                            Assembly assembly = Assembly.LoadFile(projectPlugin);
                            Type[] types = assembly.GetTypes();
                            foreach (Type type in types)
                            {
                                if (typeof(IBuilder).IsAssignableFrom(type))
                                {
                                    configuration.Builder = Activator.CreateInstance(type) as IBuilder;
                                }
                            }
                        }
                        catch (FileNotFoundException e)
                        {
                            throw new FileNotFoundException("Can not load project plugin", projectPlugin);
                        }


                        if (configuration.Builder == null)
                        {
                            throw new EntryPointNotFoundException("Can not find IBuilder class in plugin " + configuration.Plugin);
                        }


                        Console.WriteLine("[{0}] Run configuration - {1}", DateTime.Now.ToString(), Path.GetFileName(configFile));

                        this.RunSequence(configuration);

                    } // end project config loop

                } // End project loop

            }

        }


        /// <summary>
        /// Runs the sequence.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        private void RunSequence(Configuration configuration)
        {

            try
            {
                try
                {
                    if (string.IsNullOrEmpty(configuration.FTPHost))
                    {
                        return;
                    }

                    this.CheckServerAccess(configuration.FTPHost, configuration.FTPUserName, configuration.FTPPassword);
                }
                catch (InvalidOperationException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    DeamonProgram.WriteToLog(string.Format("{0}", e.Message));
                    Console.WriteLine("[{0}] {1}", DateTime.Now.ToString(), e.Message);
                    Console.ResetColor();
                    return;
                }
                Console.WriteLine("[{0}] Server Online, {1}", DateTime.Now.ToString(), configuration.FTPHost);

                // Get available versions in Source archive
                List<Version> versions = this.GetAvailableVersionsInSourceArchive(Environment.ExpandEnvironmentVariables(configuration.SourceArchive));


                // Get available binary versions in FTP Archive
                foreach (Version version in versions)
                {
                    if (this.bExit == true)
                    {
                        return;
                    }

                    // Check Class Documentation
                    if (configuration.IsGenerateClassAPI)
                    {
                        bool bClassDocumentationExists = this.CheckIfClassDocumentationExistsOnFTPServer(version,
                            Environment.ExpandEnvironmentVariables(configuration.FTPClassDocumentationPath),
                            Environment.ExpandEnvironmentVariables(configuration.FTPHost),
                            Environment.ExpandEnvironmentVariables(configuration.FTPUserName),
                            Environment.ExpandEnvironmentVariables(configuration.FTPPassword));

                        if (bClassDocumentationExists == false)
                        {
                            try
                            {
                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, Uploading class Documentation for version {2}",configuration.ProjectName, configuration.Title, version));
                                this.UploadClassDocumentation(version, configuration);
                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, Upload class documentation succeeded: version {2}", configuration.ProjectName,configuration.Title, version));
                            }
                            catch (WebException e)
                            {
                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, {2}", configuration.ProjectName,configuration.Title, e.Message));
                            }
                        }
                    }


                    // Retrive needed instances
                    GetNeededInstancesFromFTPServer task = new GetNeededInstancesFromFTPServer(version,
                        Environment.ExpandEnvironmentVariables(configuration.FTPBinaryPath),
                        Environment.ExpandEnvironmentVariables(configuration.FTPHost),
                        Environment.ExpandEnvironmentVariables(configuration.FTPUserName),
                        Environment.ExpandEnvironmentVariables(configuration.FTPPassword));

                    task.Execute();

                    int num = task.NeededInstances;

                    if (num > 0)
                    {
                        DeamonProgram.WriteToLog(string.Format("{0} - {1}, {2} Needed version {3}", configuration.ProjectName, configuration.Title, num, version));
                    }

                    // We need to build one or more instances
                    for (int i = 0; i < num; i++)
                    {
                        if (this.bExit == true)
                        {
                            return;
                        }

                        // Get and lock instance
                        VersionInfo versionInfo = Program.GetInstance(version, Environment.ExpandEnvironmentVariables(configuration.BinaryArchive));

                        if (versionInfo == null)
                        {
                            try
                            {
                                // Build one instance
                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, Building version {2}", configuration.ProjectName, configuration.Title, version));
                                if (Program.Build(version, configuration) == null)
                                {
                                    DeamonProgram.WriteToLog(string.Format("{0} - {1}, Can not claim source version {2} (source busy or not okey)", configuration.ProjectName, configuration.Title, version));
                                }
                                else
                                {
                                    DeamonProgram.WriteToLog(string.Format("{0} - {1}, Build succeeded: version {2}", configuration.ProjectName, configuration.Title, version));
                                }
                            }
                            catch (Exception e)
                            {
                                string message = e.Message;
                                if (e is FileNotFoundException)
                                {
                                    message += " : " + ((FileNotFoundException)e).FileName;
                                }

                                Console.ForegroundColor = ConsoleColor.Red;
                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, {2}", configuration.ProjectName, configuration.Title, message));
                                Console.WriteLine("[{0}] {1}", DateTime.Now.ToString(), message);
                                Console.ResetColor();
                                return;
                            }

                            // Get and lock instance
                            versionInfo = Program.GetInstance(version, Environment.ExpandEnvironmentVariables(configuration.BinaryArchive));
                        }

                        if (versionInfo != null)
                        {

                            if (string.IsNullOrEmpty(configuration.FTPHost))
                            {
                                // Do not upload
                                continue;
                            }

                            DeamonProgram.WriteToLog(string.Format("{0} - {1}, Uploading version {2}", configuration.ProjectName, configuration.Title, version));

                            // Upload instance

                            try
                            {

                                this.UploadInstance(versionInfo,
                                    Environment.ExpandEnvironmentVariables(configuration.FTPBinaryPath),
                                    Environment.ExpandEnvironmentVariables(configuration.FTPHost),
                                    Environment.ExpandEnvironmentVariables(configuration.FTPUserName),
                                    Environment.ExpandEnvironmentVariables(configuration.FTPPassword));

                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, Upload succeeded: version {2}", configuration.ProjectName, configuration.Title, version));

                                // Remove uploaded instance from Binary Archive
                                Program.RemoveInstanceFromBinaryArchive(versionInfo);
                                this.dynamicLoopDelay = 1000 * 60 * 1; // 1 min;
                            }
                            catch (WebException e)
                            {
                                // Upload failed, unlock instance

                                VersionInfo.UnLockInstance(versionInfo);

                                DeamonProgram.WriteToLog(string.Format("{0} - {1}, {2}", configuration.ProjectName, configuration.Title, e.ToString()));

                                // Increase loop cycle becoz the FTP is offline/unavilable/access denied/etc..
                                this.dynamicLoopDelay = 1000 * 60 * 10; // 10 min;
                                return; // Stop looping version
                            }

                        }
                    }

                }


            }
            catch (Exception e)
            {
                DeamonProgram.WriteToLog(string.Format("{0} - {1}, StackTrace: {2}", configuration.ProjectName, configuration.Title, e.ToString()));

                throw e;
            }



        }


        private void CheckServerAccess(string ftpHost, string username, string password)
        {

            Uri host = new Uri(ftpHost);
            FtpManager ftpClient = new FtpManager(host, username, password);

            try
            {
                ftpClient.CheckServerAccess();
            }
            catch (WebException e)
            {
                char[] charToRemove = { '\r', '\n' };
                string message = ((FtpWebResponse)e.Response).StatusDescription;
                throw new InvalidOperationException(string.Format("Can not access FTP server. {0} - {1}", ftpHost, message).Trim(charToRemove));
            }

        }


        /// <summary>
        /// Gets the available sources in Source Archive.
        /// </summary>
        /// <remarks>
        /// Returns a list of vaild version folder names in Source archive (where the 'version'.prepared file exists.)
        /// </remarks>
        /// <returns>Returns a list of vaild version folder names in Source archive.</returns>
        private List<Version> GetAvailableVersionsInSourceArchive(string sourceArchive)
        {
            List<Version> versions = new List<Version>();

            //// Only look for ".ok" source files (Not .Busy )
            //string[] files = Directory.GetFiles(sourceArchive);
            //foreach (string file in files)
            //{
            //    if (!file.EndsWith(".ok", StringComparison.CurrentCultureIgnoreCase))
            //    {
            //        continue;
            //    }

            //    string versionString = Path.GetFileNameWithoutExtension(file);

            //    string instance = Path.Combine(sourceArchive, versionString);

            //    try
            //    {
            //        if (Directory.Exists(instance))
            //        {

            //            Version version = new Version(versionString);

            //            // TODO: Maybe also check for valid content?

            //            versions.Add(version);
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        // Ignore not valid folders
            //    }

            //}

            //return versions;


            if (!Directory.Exists(sourceArchive))
            {
                return versions;
            }

            // TODO: Use DirectoryInfo ?
            string[] directories = Directory.GetDirectories(sourceArchive);

            foreach (string directory in directories)
            {

                try
                {
                    // Check if valid directory

                    string[] folders = directory.Split(new char[2] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                    if (folders == null || folders.Length == 0) continue;

                    // Get Last folder
                    string folderName = folders[folders.Length - 1];
                    Version version = new Version(folderName);


                    // Check if <version>.ok file exists
                    string doneFile = Path.Combine(sourceArchive, version.ToString());
                    doneFile = doneFile + ".ok";

                    if (File.Exists(doneFile))
                    {
                        versions.Add(version);
                    }

                    // TODO: Maybe also check for valid content?
                }
                catch (Exception)
                {
                    // Ignore not valid folders
                }
            }
            return versions;
        }


        /// <summary>
        /// Uploads the instance.
        /// </summary>
        /// <param name="versionInfo">The version info.</param>
        /// <param name="ftpBinaryPath">The FTP binary path.</param>
        /// <param name="ftpHost">The FTP host.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        private void UploadInstance(VersionInfo versionInfo, string ftpBinaryPath, string ftpHost, string username, string password)
        {

            UploadInstanceToFTPServer task = new UploadInstanceToFTPServer(versionInfo,
                ftpHost,
                ftpBinaryPath,
                username,
                password);

            task.Execute();

        }


        private bool CheckIfClassDocumentationExistsOnFTPServer(Version version, string ftpDocPath, string ftpHost, string username, string password)
        {
            Uri host = new Uri(ftpHost);
            FtpManager ftpClient = new FtpManager(host, username, password);

            ftpDocPath = Path.Combine(ftpDocPath, version.ToString());
            string doneFile = Path.Combine(ftpDocPath, "uploaded.ok");
            return ftpClient.FileExists(doneFile);
        }


        private void UploadClassDocumentation(Version version, Configuration configuration)
        {

            string docFolder = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.BinaryArchive), version.ToString());

            docFolder = Path.Combine(docFolder, "Documentation");
            docFolder = Path.Combine(docFolder, "ClassAPI");

            string ftpDocPath = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.FTPClassDocumentationPath), version.ToString());

            try
            {

                UploadClassAPItoFTPServer task = new UploadClassAPItoFTPServer(version,
                    docFolder,
                    ftpDocPath,
                    Environment.ExpandEnvironmentVariables(configuration.FTPHost),
                    Environment.ExpandEnvironmentVariables(configuration.FTPUserName),
                    Environment.ExpandEnvironmentVariables(configuration.FTPPassword));

                task.Execute();

                // TODO: Make a own task of this? (so we can rollback in the future etc..)
                // Create Link do cocumentation  " 
                string key = "bb9baa37ce2cf973e11fdeb40c137c718e69ceb30cffc264ac090a9cbcdae575";
                string cmd = string.Format(@"http://www.rapidminds.com/Documentation/RapidMinds/" + configuration.Builder.SolutionName + "@/mklink.php?v={0}&key={1}", version.ToString(), key);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(cmd);
                request.Credentials = new NetworkCredential(configuration.FTPUserName, configuration.FTPPassword);
                //request.KeepAlive = false;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();

            }
            catch (DirectoryNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0}", e.Message);
                Console.ResetColor();
            }

        }


        public static void WriteToLog(string message)
        {
            // create a writer and open the file
            Assembly assembly = Assembly.GetExecutingAssembly();

            string logFile = Path.GetDirectoryName(assembly.Location);

            logFile = Path.Combine(logFile, "Deamon.log");

            TextWriter textWriter = new StreamWriter(logFile, true);

            // write a line of text to the file
            textWriter.WriteLine("[{0}] {1}", DateTime.Now.ToString(), message);

            // close the stream
            textWriter.Close();
        }


    }



    [Serializable]
    public class DeamonConfiguration
    {

        public int Frequence { get; set; }
        public bool IsEnabled { get; set; }
        public List<Project> Projects { get; set; }
    }

    [Serializable]
    public class Project
    {

        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsEnabled { get; set; }

    }

}
