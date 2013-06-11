﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Diagnostics;
using BuildSystemHelper;
using System.Threading;
using System.Reflection;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace GenerateInstaller
{
    class GenerateInstaller
    {
        static String buildsFTPPoolDir = BuildSystem.StableBuildsName;
        const String randHistoryFileName = "RandHistory.txt";
        const String licenseDownloadIDPattern = "@UniqueDownloadKey";
        const String licenseDatePattern = "@RequiredRegistrationDate";
        const Int32 DownloadKeyLengthBytes = 15;

        const String companyName = "Starcounter AB";
        const String productName = "Starcounter Components";
        public static readonly String certificateFile = BuildSystem.LocalToolsFolder + "\\starcounter-2014.cer";

        /// <summary>
        /// Directories in output that should be deleted before packaging into one installer.
        /// </summary>
        static readonly String[] OutputDirsToDelete =
        {
            ".db",
            ".db.output",
            ".srv",
            "s",
            "NetworkIoTest"
        };

        /// <summary>
        /// Files in output that should be deleted before packaging into one installer.
        /// </summary>
        static readonly String[] OutputFilesToDelete =
        {
            
        };

        // Uploads build on FTP.
        static void UploadBuildToFtp(
            String buildType,
            String versionNumber,
            String fileName,
            String fileLocalPath)
        {
            if ((buildType != BuildSystem.TestBetaName) &&
                (buildType != BuildSystem.StableBuildsName) &&
                (buildType != BuildSystem.NightlyBuildsName) &&
                (buildType != BuildSystem.CustomBuildsName))
            {
                throw new Exception("Wrong argument: " + buildType + ".");
            }

            String relativeFilePathWithSlashes = Path.Combine(Path.Combine(buildType, versionNumber), fileName).Replace("\\", "/");

            BuildSystem.UploadFileToFTP(
                BuildSystem.StarcounterFtpConfigName,
                fileLocalPath,
                relativeFilePathWithSlashes);
        }

        // Replaces string in file.
        static void ReplaceStringInFile(String filePath, String origStringRegex, String replaceString)
        {
            String fileContents = File.ReadAllText(filePath);

            Match match = Regex.Match(fileContents, origStringRegex, RegexOptions.IgnoreCase);

            // Trying to find this exact string in file.
            if (!match.Success)
                throw new Exception("Can't find matching string " + origStringRegex + " in file " + filePath);

            fileContents = fileContents.Replace(match.Value, replaceString);

            File.WriteAllText(filePath, fileContents);
        }

        // Accepts builds pool FTP mapped directory as a path
        // ('StableBuilds' or 'NightlyBuilds' without version part).
        static int Main(string[] args)
        {
            String licenseFilePath = null,
               licensePatternContents = null,
               installerWpfFolder = null;

            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Starcounter Installer Generator");

                // Checking if same executable is already running.
                if (BuildSystem.IsSameExecutableRunning())
                    return 0;

                // Checking if its a personal build.
                if (Environment.GetEnvironmentVariable(BuildSystem.GenerateInstallerEnvVar) != "True")
                {
                    Console.WriteLine("Skipping generation of a installer...");
                    return 0;
                }

                // Processing parameters.
                foreach (String arg in args)
                {
                    // Seems that specific pool folder is an argument.
                    buildsFTPPoolDir = arg;

                    Console.WriteLine("Changed FTP mapped sub-folder to " + buildsFTPPoolDir);
                    Thread.Sleep(1000);
                }

                DownloadID downloadID = new DownloadID();

                ////////////////////////////////////////////
                // Getting values for environment variables.
                ////////////////////////////////////////////
                Console.WriteLine("Obtaining needed environment variables...");

                // Getting current build configuration.
                String configuration = Environment.GetEnvironmentVariable("Configuration");
                if (configuration == null)
                {
                    throw new Exception("Environment variable 'Configuration' does not exist.");
                }

                // Getting current build platform.
                String platform = "x64"; // Environment.GetEnvironmentVariable("Platform");
                if (platform == null)
                {
                    throw new Exception("Environment variable 'Platform' does not exist.");
                }

                // Getting current build version.
                String version = Environment.GetEnvironmentVariable(BuildSystem.BuildNumberEnvVar);
                if (version == null)
                {
                    throw new Exception("Environment variable 'BUILD_NUMBER' does not exist.");
                }

                // Getting sources directory.
                String sourcesDir = Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar);
                if (sourcesDir == null)
                {
                    throw new Exception("Environment variable 'WORKSPACE' does not exist.");
                }

                // Indicating that we want to skip local FTP.
                Environment.SetEnvironmentVariable(BuildSystem.BuildSystemSkipLocalFtp, "True");
                
                // Getting the path to current build consolidated folder.
                String outputFolder = Path.Combine(sourcesDir, "Level1\\Bin\\" + configuration);

                if (!Directory.Exists(outputFolder))
                    throw new Exception("Consolidated directory is empty or does not exist: " + outputFolder);

                //////////////////////////////////////////////////////////
                // Empirically generating new random download identifier.
                //////////////////////////////////////////////////////////

                // Adding the XML header.
                String versionFileContents = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + Environment.NewLine;

                // Adding header version tag.
                versionFileContents += "<VersionInfo>" + Environment.NewLine;

                // Adding current build configuration.
                versionFileContents += "  <Configuration>" + configuration + "</Configuration>" + Environment.NewLine;

                // Adding current build platform.
                versionFileContents += "  <Platform>" + platform + "</Platform>" + Environment.NewLine;

                // Adding current build version.
                versionFileContents += "  <Version>" + version + "</Version>" + Environment.NewLine;

                // Adding current build unique identifier in Base32 format.
                versionFileContents += "  <IDFullBase32>" + downloadID.IDFullBase32 + "</IDFullBase32>" + Environment.NewLine;

                // Adding converted postfix bytes in Base64 format.
                versionFileContents += "  <IDTailBase64>" + downloadID.IDTailBase64 + "</IDTailBase64>" + Environment.NewLine;
                versionFileContents += "  <IDTailDecimal>" + downloadID.IDTailDecimal + "</IDTailDecimal>" + Environment.NewLine;

                // Adding required registration date.
                String requiredRegistrationDate = DateTime.Now.AddDays(60).ToString("yyyy-MM-dd").ToUpper();
                versionFileContents += "  <RequiredRegistrationDate>" + requiredRegistrationDate + "</RequiredRegistrationDate>" + Environment.NewLine;

                // Adding closing tag.
                versionFileContents += "</VersionInfo>" + Environment.NewLine;

                // Updating installer engine resources.
                String installerEngineFolder = Path.Combine(sourcesDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerEngine");

                BuildSystem.SetNormalDirectoryAttributes(new DirectoryInfo(installerEngineFolder));

                // Updating the version information.
                File.WriteAllText(Path.Combine(installerEngineFolder, BuildSystem.VersionXMLFileName), versionFileContents);

                // Replacing version information.
                String currentVersionFilePath = Path.Combine(sourcesDir, @"Level1\src\Starcounter.Internal\Constants\CurrentVersion.cs");
                ReplaceStringInFile(currentVersionFilePath, @"String Version = ""[0-9\.]+"";", "String Version = \"" + version + "\";");
                ReplaceStringInFile(currentVersionFilePath, @"String IDFullBase32 = ""[0-9]+"";", "String IDFullBase32 = \"" + downloadID.IDFullBase32 + "\";");
                ReplaceStringInFile(currentVersionFilePath, @"String IDTailBase64 = ""[0-9]+"";", "String IDTailBase64 = \"" + downloadID.IDTailBase64 + "\";");
                ReplaceStringInFile(currentVersionFilePath, @"UInt32 IDTailDecimal = [0-9]+;", "UInt32 IDTailDecimal = " + downloadID.IDTailDecimal + ";");
                ReplaceStringInFile(currentVersionFilePath, @"DateTime RequiredRegistrationDate = DateTime\.Parse\(""[0-9\-]+""\);", "DateTime RequiredRegistrationDate = DateTime.Parse(\"" + requiredRegistrationDate + "\");");

                //////////////////////////////////////////////////////////
                // Packaging consolidated folder, updating resources, etc.
                //////////////////////////////////////////////////////////
                Console.WriteLine("Updating resources and building empty installer...");

                // Checking if setup file already exists.
                String specificSetupFileName = "Starcounter-" + version + "-Setup.exe";
                String specificSetupFilePath = Path.Combine(outputFolder, specificSetupFileName);
                String staticSetupFileName = "Starcounter-Setup.exe";
                String staticSetupFilePath = Path.Combine(outputFolder, staticSetupFileName);

                Console.WriteLine("Removing old setup file...");
                BuildSystem.DirectoryContainsFilesRegex(outputFolder, new String[] { @"Starcounter.+Setup\.exe" }, true);

                // Looking for an Installer WPF resources folder.
                installerWpfFolder = Path.Combine(sourcesDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerWPF");

                BuildSystem.SetNormalDirectoryAttributes(new DirectoryInfo(installerWpfFolder));

                // Saving the new license information.
                licenseFilePath = Path.Combine(installerWpfFolder, "Resources\\LicenseAgreement.html");
                licensePatternContents = File.ReadAllText(licenseFilePath);

                // Replacing license file with a new unique version.
                String modifiedLicense = licensePatternContents.Replace(licenseDownloadIDPattern, downloadID.IDFullBase32);

                // Creating required registration date.
                String currentDatePlus60Days = DateTime.Now.AddDays(60).ToString("MMMM dd, yyyy").ToUpper();
                File.WriteAllText(licenseFilePath, modifiedLicense.Replace(licenseDatePattern, currentDatePlus60Days));

                // Restoring fake archive file if needed.
                File.WriteAllText(Path.Combine(installerWpfFolder, "Resources\\Archive.zip"), "This is an empty file...");

                // Now compiling the Installer WPF project.
                ProcessStartInfo msbuildInfo = new ProcessStartInfo();
                msbuildInfo.FileName = BuildSystem.MsBuildExePath;
                String msbuildArgs = "\"" + Path.Combine(sourcesDir, @"Level1\src\Level1.sln") + "\"" + " /maxcpucount /NodeReuse:false /target:Build /property:Configuration=" + configuration + ";Platform=" + platform;
                msbuildInfo.Arguments = msbuildArgs;
                msbuildInfo.WorkingDirectory = outputFolder;
                msbuildInfo.UseShellExecute = false;

                // Start the Installer WPF.
                Process msbuildProcess = Process.Start(msbuildInfo);
                msbuildProcess.WaitForExit();
                if (msbuildProcess.ExitCode != 0)
                {
                    throw new Exception("Building Installer WPF project failed with error code: " + msbuildProcess.ExitCode);
                }
                msbuildProcess.Close();

                // Collecting the list of executables and libraries in order to sign them.
                String[] allFilesToSign = Directory.GetFiles(outputFolder, "*.exe");
                String signingError = "error";

                // Trying to sign several times.
                signingError = BuildSystem.SignFiles(allFilesToSign, companyName, productName, certificateFile);

                // Checking if there are any errors during signing process.
                if (signingError != null)
                    throw new Exception("Failed to sign files:" + Environment.NewLine + signingError);

                Console.WriteLine("Deleting selected directories and files...");

                // Removing selected directories from output.
                foreach (String delDir in OutputDirsToDelete)
                {
                    String delDirPath = Path.Combine(outputFolder, delDir);

                    if (Directory.Exists(delDirPath))
                    {
                        Directory.Delete(delDirPath, true);

                        Console.WriteLine("  Deleted directory: " + delDirPath);
                    }
                }

                // Removing selected files from output.
                foreach (String delFile in OutputFilesToDelete)
                {
                    String delFilePath = Path.Combine(outputFolder, delFile);

                    if (File.Exists(delFilePath))
                    {
                        File.Delete(delFilePath);

                        Console.WriteLine("  Deleted file: " + delFilePath);
                    }
                }

                // Now packing everything into one big ZIP archive.
                Console.WriteLine("Packaging consolidated folder and building complete installer...");

                String archivePath = Path.Combine(installerWpfFolder, "Resources\\Archive.zip");
                if (File.Exists(archivePath))
                    File.Delete(archivePath);

                // Zipping whole consolidated directory.
                ZipFile.CreateFromDirectory(outputFolder, archivePath, CompressionLevel.Optimal, false);

                // Compiling second time with archive.
                msbuildInfo.Arguments = msbuildArgs;
                msbuildProcess = Process.Start(msbuildInfo);
                msbuildProcess.WaitForExit();
                if (msbuildProcess.ExitCode != 0)
                {
                    throw new Exception("Building Installer WPF project has failed with error code: " + msbuildProcess.ExitCode);
                }
                msbuildProcess.Close();

                // Signing the main Starcounter setup.
                signingError = BuildSystem.SignFiles(new String[] { staticSetupFilePath }, companyName, productName, certificateFile);

                // Checking if there are any errors during signing process.
                if (signingError != null)
                    throw new Exception("Failed to sign static Starcounter setup file...");

                Console.WriteLine("Building installer wrapper...");

                // Copying necessary embedded files.
                String installerWrapperDir = Path.Combine(sourcesDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerWrapper");

                File.Copy(Path.Combine(BuildSystem.MappedBuildServerFTP, @"SCDev\ThirdParty\dotNET\dotnetfx45_full_x86_x64.exe"),
                    Path.Combine(installerWrapperDir, "resources", "dotnetfx45_full_x86_x64.exe"), true);

                File.Copy(staticSetupFilePath, Path.Combine(installerWrapperDir, "resources", staticSetupFileName), true);

                String installerWrapperVersionFilePath = Path.Combine(installerWrapperDir, "Starcounter.InstallerWrapper.cs");

                // Setting current installer version.
                ReplaceStringInFile(installerWrapperVersionFilePath, @"String ScSetupVersion = ""[0-9\.]+"";", "String ScSetupVersion = \"" + version + "\";");

                // Compiling second time with archive.
                msbuildArgs = "\"" + Path.Combine(sourcesDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerWrapper\Starcounter.InstallerWrapper.csproj") + "\"" + " /maxcpucount /NodeReuse:false /target:Build /property:Configuration=" + configuration + ";Platform=AnyCPU";
                msbuildInfo.Arguments = msbuildArgs + ";SC_CREATE_STANDALONE_SETUP=True";
                msbuildProcess = Process.Start(msbuildInfo);
                msbuildProcess.WaitForExit();
                if (msbuildProcess.ExitCode != 0)
                {
                    throw new Exception("Building installer wrapper project has failed with error code: " + msbuildProcess.ExitCode);
                }
                msbuildProcess.Close();

                // Signing the main Starcounter setup.
                signingError = BuildSystem.SignFiles(new String[] { specificSetupFilePath }, companyName, productName, certificateFile);

                // Checking if there are any errors during signing process.
                if (signingError != null)
                    throw new Exception("Failed to sign specific Starcounter setup file...");

                // Uploading changes to FTP server (only if its not a personal build).
                if (Environment.GetEnvironmentVariable(BuildSystem.UploadToUsFtp) == "True")
                {
                    // Checking if its a releasing build.
                    if (BuildSystem.IsReleasingBuild())
                    {
                        UInt32 previousRandomNumCount = downloadID.Generate(BuildSystem.StarcounterFtpConfigName, DownloadKeyLengthBytes, randHistoryFileName);

                        // Creating this build version folder if needed.
                        buildsFTPPoolDir = buildsFTPPoolDir + "/" + version;
                        Console.WriteLine("Uploading everything to FTP server mapped folder: " + buildsFTPPoolDir);

                        // Updating the random history file.
                        String existingRandHistory = BuildSystem.GetFTPFileAllText(BuildSystem.StarcounterFtpConfigName, randHistoryFileName);
                        BuildSystem.UploadFileTextToFTP(BuildSystem.StarcounterFtpConfigName, existingRandHistory + Environment.NewLine + downloadID.IDFullBase32, randHistoryFileName);

                        // Copying file to destination directory.
                        String targetBase64Dir = buildsFTPPoolDir + "/" + downloadID.IDTailBase64;
                        BuildSystem.UploadFileToFTP(BuildSystem.StarcounterFtpConfigName, specificSetupFilePath, targetBase64Dir + "/" + specificSetupFileName);

                        // Saving the new build version information as a last step.
                        BuildSystem.UploadFileTextToFTP(BuildSystem.StarcounterFtpConfigName, versionFileContents, buildsFTPPoolDir + "/" + "VersionInfo_" + previousRandomNumCount + ".xml");

                        Console.WriteLine("Done uploading to FTP server...");
                    }

                    // Uploading standalone setup to FTP.
                    if (args.Length > 0)
                        UploadBuildToFtp(args[0], version, specificSetupFileName, specificSetupFilePath);
                }

                Console.WriteLine("Succeeded generating unique installer with download id: " + downloadID.IDFullBase32);
                Console.WriteLine("---------------------------------------------------------------");

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
            finally
            {
                if ((licenseFilePath != null) && (licensePatternContents != null))
                {
                    // Restoring original license file contents.
                    File.WriteAllText(licenseFilePath, licensePatternContents);
                }

                // Restoring fake archive file.
                if (installerWpfFolder != null)
                {
                    File.WriteAllText(Path.Combine(installerWpfFolder, "Resources\\Archive.zip"), "This is an empty file...");
                }
            }
        }
    }
}
