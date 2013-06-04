using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using BuildSystemHelper;
using System.Reflection;

namespace BuildsFillupDaemon
{
    class BuildsFillupDaemon
    {
        // Paths and filenames constants.
        static String buildsFolderName = BuildSystem.CustomBuildsName;
        static String genInstallerExe = Path.Combine(BuildSystem.GetAssemblyDir(), "GenerateInstaller.exe");

        // Accepts builds pool FTP mapped directory as a path
        // ('StableBuilds' or 'NightlyBuilds' without version part).
        static Int32 Main(String[] args)
        {
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Build FTP Fillup Daemon");

                // Checking if same executable is already running.
                if (BuildSystem.IsSameExecutableRunning())
                    return 0;

                // Current directory must contain sub-directory 'Level1\Src\...'
                String sourcesDir = BuildSystem.GetAssemblyDir();

                String configuration = null,
                        platform = null,
                        version = null;

                // Load the XML document from the specified version file.
                String xmlVersionFilePath = Path.Combine(sourcesDir, BuildSystem.VersionXMLFileName);
                if (!File.Exists(xmlVersionFilePath))
                {
                    throw new Exception("Needed configuration file does not exist: " + xmlVersionFilePath);
                }

                // Reading the build version information XML file.
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlVersionFilePath);

                XmlNodeList configurationTags = xmlDoc.GetElementsByTagName("Configuration");
                configuration = ((XmlElement) configurationTags[0]).InnerText; // e.g. Release

                XmlNodeList platformTags = xmlDoc.GetElementsByTagName("Platform");
                platform = ((XmlElement) platformTags[0]).InnerText; // e.g. x64

                XmlNodeList versionTags = xmlDoc.GetElementsByTagName("Version");
                version = ((XmlElement) versionTags[0]).InnerText; // e.g. 2.0.0.0

                XmlNodeList buildsFolderNameTags = xmlDoc.GetElementsByTagName("BuildsFolderName");
                buildsFolderName = ((XmlElement)buildsFolderNameTags[0]).InnerText; // e.g. StableBuilds

                // Diagnostic message.
                Console.Error.WriteLine("Changed FTP mapped sub-folder to " + buildsFolderName + "...");

                // Checking that major parameters are set.
                if ((configuration == null) || (platform == null) || (version == null))
                {
                    throw new Exception("Configuration, platform or version parameters are not set. Quiting...");
                }

                // Setting all needed build environment variables.
                Environment.SetEnvironmentVariable("Configuration", configuration);
                //Environment.SetEnvironmentVariable("Platform", platform);
                Environment.SetEnvironmentVariable(BuildSystem.BuildNumberEnvVar, version);
                Environment.SetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar, sourcesDir);

                // Indicating that we need to upload to US FTP.
                Environment.SetEnvironmentVariable(BuildSystem.UploadToUsFtp, "True");

                // Setting this since all pre-compiled files are already copied.
                Environment.SetEnvironmentVariable("DONT_COPY_EXTERNAL_FILES", "True");

                // Checking that all needed variables are defined.
                if (!BuildSystem.AllEnvVariablesExist(new String[]
                {
                    "Configuration",
                    //"Platform",
                    BuildSystem.BuildNumberEnvVar,
                    BuildSystem.CheckOutDirEnvVar
                }))
                {
                    throw new Exception("Not all environment variables exist.");
                }

                // Going into infinite cycle as a daemon.
                String buildsNeededFTPFilePath = buildsFolderName + "/" + version + "/NeededCount.txt";

                // Trying to read the value of the amount of needed builds.
                try { BuildSystem.GetFTPFileAllText(BuildSystem.StarcounterFtpConfigName, buildsNeededFTPFilePath); }
                catch
                {
                    // Fetching the default pool size.
                    String defaultPoolSize = BuildSystem.GetFTPFileAllText(BuildSystem.StarcounterFtpConfigName, "PoolSize.txt");

                    // The file is not created we need to create a new one.
                    BuildSystem.UploadFileTextToFTP(BuildSystem.StarcounterFtpConfigName, defaultPoolSize, buildsNeededFTPFilePath);
                }

                while (true)
                {
                    // Checking if there is a stop file placed in the directory.
                    if (File.Exists(Path.Combine(BuildSystem.GetAssemblyDir(), BuildSystem.StopDaemonFileName)))
                        return 0;

                    // Checking how many builds we need to create.
                    String neededCountString = BuildSystem.GetFTPFileAllText(BuildSystem.StarcounterFtpConfigName, buildsNeededFTPFilePath);
                    Int32 neededCount = Int32.Parse(neededCountString);

                    // Checking if we need to create new builds.
                    if (neededCount > 0)
                    {
                        // Printing diagnostic message.
                        Console.Error.WriteLine("---------------------------------------------------------------");
                        Stopwatch timer = Stopwatch.StartNew();

                        // We need to fill up the build pool again.
                        ProcessStartInfo genInstallerInfo = new ProcessStartInfo();
                        genInstallerInfo.FileName = "\"" + genInstallerExe + "\"";
                        genInstallerInfo.Arguments = "\"" + buildsFolderName + "\""; // Specifying the target USA root pool folder.
                        genInstallerInfo.UseShellExecute = false;

                        // Status update.
                        Console.Error.WriteLine("Starting new installer generation: " + genInstallerInfo.FileName + " " + genInstallerInfo.Arguments);

                        // Start the Installer generation.
                        Process genInstallerProc = Process.Start(genInstallerInfo);
                        genInstallerProc.WaitForExit();
                        if (genInstallerProc.ExitCode != 0)
                        {
                            Console.Error.WriteLine("Failed generating installer. Trying next one in 3 seconds...");
                            genInstallerProc.Close();
                            Thread.Sleep(3000);
                            continue;
                        }
                        genInstallerProc.Close();

                        // Printing diagnostic message.
                        timer.Stop();
                        Console.Error.WriteLine("Successfully created new unique installer: " + configuration + " " + version + " in " + timer.ElapsedMilliseconds + " ms.");
                        Console.Error.WriteLine("---------------------------------------------------------------");

                        // Decreasing the needed amount of builds by one and updating the server.
                        neededCountString = null;
                        neededCountString = BuildSystem.GetFTPFileAllText(BuildSystem.StarcounterFtpConfigName, buildsNeededFTPFilePath);
                        neededCount = Int32.Parse(neededCountString);
                        if (neededCount > 0)
                        {
                            neededCount--;
                            BuildSystem.UploadFileTextToFTP(BuildSystem.StarcounterFtpConfigName, neededCount.ToString(), buildsNeededFTPFilePath);
                        }
                    }

                    // Time interval to wait until next pool check.
                    Thread.Sleep(5000);
                }
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
