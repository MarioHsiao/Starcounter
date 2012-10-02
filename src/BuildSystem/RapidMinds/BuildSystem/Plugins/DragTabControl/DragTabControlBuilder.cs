using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RapidMinds.BuildSystem.Common;
using RapidMinds.BuildSystem.Common.Tasks;
using RapidMinds.BuildSystem.Common.Tools;
using Plugin.DragTabControl.Tasks;
using System.Security.Cryptography;
using System.Globalization;
using System.Reflection.Emit;
using System.Reflection;

namespace Plugin.DragTabControl
{
    public class DragTabControlBuilder : IBuilder
    {

        #region Properties

        public string SolutionName
        {
            get { return "DragTabControl"; }
        }

        #endregion

        public DragTabControlBuilder()
        {
        }


        /// <summary>
        /// Prepares the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public void Prepare(Configuration configuration)
        {

            // Preparation steps
            this.Preparation_Step_1_GenerateNextVersionNumber(configuration);

            try
            {
                this.Preparation_Step_2_LabelSourceInPerforce(configuration);
                this.Preparation_Step_3_CopySourceFilesToSourceArchive(configuration);
                this.Preparation_Step_4_SetVersonNumberToSourceFiles(configuration);
                this.Preparation_Step_5_CreateChangeLog(configuration);
            }
            catch (Exception e)
            {
                this.Rollback_Step_2_LabelSourceInPerforce(configuration);
                throw e;
            }

        }

        /// <summary>
        /// Builds the specified version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        public VersionInfo Build(Version version, Configuration configuration)
        {
            configuration.Version = version;    // TODO: Remove Version from configuration

            try
            {
                // Build steps
                this.Build_Step_1_CleanBinariesFromSourceArchive(configuration);
                this.Build_Step_2_GenerateSerialInformation(configuration);
                this.Build_Step_3_SetSerialInformationToSourceFiles(configuration);
                this.Build_Step_4_SetSerialInformationToLicenseAgreement(configuration);
                this.Build_Step_5_ModifyInstallShieldProjectFile(configuration);
                this.Build_Step_6_BuildDragTabControlSolution(configuration);
                this.Build_Step_7_RunUnitTests(configuration);

                if (configuration.IsGenerateClassAPI)
                {
                    this.Build_Step_8_GenerateClassDocumentation(configuration);
                }
                VersionInfo versionInfo = this.Build_Step_9_CopyBinariesToBinaryArchive(configuration);
                return versionInfo;
            }
            catch (Exception e)
            {
                this.Rollback_Step_2_LabelSourceInPerforce(configuration);
                throw e;
            }

        }

        #region Preparations Steps

        private void Preparation_Step_1_GenerateNextVersionNumber(Configuration configuration)
        {
            GenerateNextVersionNumber task = new GenerateNextVersionNumber("DragTabControl_LatestVersion.txt");

            task.Execute();

            configuration.Version = task.NextVersion;

            Console.WriteLine("##teamcity[buildNumber '{0}']", configuration.Version.ToString());

        }

        private void Preparation_Step_2_LabelSourceInPerforce(Configuration configuration)
        {

            if (configuration.IsSetPerforceLabel)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.Error.WriteLine("------ Label started: Setting label to source ------");
                Console.ResetColor();


                string perforcePassword = Environment.ExpandEnvironmentVariables(DecryptString(configuration.PerforcePassword, "dlsmcss"));

                PerforceClient pClient = new PerforceClient(Environment.ExpandEnvironmentVariables(configuration.PerforceServer),
                    int.Parse(Environment.ExpandEnvironmentVariables(configuration.PerforcePort)),
                    Environment.ExpandEnvironmentVariables(configuration.PerforceUsername),
                    perforcePassword,
                    Environment.ExpandEnvironmentVariables(configuration.PerforceLableDepot));


                pClient.SetLabel("DragTabControl - "+ configuration.Title, configuration.Version);

                // Time Elapsed 00:00:00.72
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Error.WriteLine("Label succeeded. ({0})", configuration.Version);
                Console.ResetColor();

            }


        }

        private void Rollback_Step_2_LabelSourceInPerforce(Configuration configuration)
        {
            if (configuration.IsSetPerforceLabel)
            {

                string perforcePassword = Environment.ExpandEnvironmentVariables(DecryptString(configuration.PerforcePassword, "dlsmcss"));

                PerforceClient pClient = new PerforceClient(Environment.ExpandEnvironmentVariables(configuration.PerforceServer),
                    int.Parse(Environment.ExpandEnvironmentVariables(configuration.PerforcePort)),
                    Environment.ExpandEnvironmentVariables(configuration.PerforceUsername),
                    perforcePassword,
                    Environment.ExpandEnvironmentVariables(configuration.PerforceLableDepot));

                pClient.RemoveLabel(configuration.Title, configuration.Version);
            }

        }

        private void Preparation_Step_3_CopySourceFilesToSourceArchive(Configuration configuration)
        {

            CopySourceFilesToSourceArchive task = new CopySourceFilesToSourceArchive(
                configuration.Version,
                Environment.ExpandEnvironmentVariables(configuration.CheckoutPath),
                Environment.ExpandEnvironmentVariables(configuration.SourceArchive));

            task.Execute();

        }

        private void Preparation_Step_4_SetVersonNumberToSourceFiles(Configuration configuration)
        {

            // .NET 3.5
            string assemblyFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            assemblyFile = Path.Combine(assemblyFile, "RapidMinds");
            assemblyFile = Path.Combine(assemblyFile, "DragTabControl");
            assemblyFile = Path.Combine(assemblyFile, "RapidMinds.Controls.Wpf35.DragTabControl");
            assemblyFile = Path.Combine(assemblyFile, "Properties");
            assemblyFile = Path.Combine(assemblyFile, "AssemblyInfo.cs");

            SetVersonNumberToSourceFiles task = new SetVersonNumberToSourceFiles(
                configuration.Version,
                configuration.GetFullPath( configuration.SetVersionTool),
                assemblyFile);

            task.Execute();


            // .NET 4.0
            string assemblyFile4 = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            assemblyFile4 = Path.Combine(assemblyFile4, "RapidMinds");
            assemblyFile4 = Path.Combine(assemblyFile4, "DragTabControl");
            assemblyFile4 = Path.Combine(assemblyFile4, "RapidMinds.Controls.Wpf4.DragTabControl");
            assemblyFile4 = Path.Combine(assemblyFile4, "Properties");
            assemblyFile4 = Path.Combine(assemblyFile4, "AssemblyInfo.cs");

            SetVersonNumberToSourceFiles task4 = new SetVersonNumberToSourceFiles(
                configuration.Version,
                configuration.GetFullPath(configuration.SetVersionTool),
                assemblyFile4);

            task4.Execute();


        }

        private void Preparation_Step_5_CreateChangeLog(Configuration configuration)
        {

            if (!configuration.IsChangeLog)
            {
                return;
            }

            string perforcePassword = Environment.ExpandEnvironmentVariables(DecryptString(configuration.PerforcePassword, "dlsmcss"));

            PerforceClient pClient = new PerforceClient(Environment.ExpandEnvironmentVariables(configuration.PerforceServer),
                int.Parse(Environment.ExpandEnvironmentVariables(configuration.PerforcePort)),
                Environment.ExpandEnvironmentVariables(configuration.PerforceUsername),
                perforcePassword,
                Environment.ExpandEnvironmentVariables(configuration.PerforceLableDepot));


            string changeLogFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            changeLogFile = Path.Combine(changeLogFile, "RapidMinds");
            changeLogFile = Path.Combine(changeLogFile, "DragTabControl");
            changeLogFile = Path.Combine(changeLogFile, "RapidMinds.Controls.Wpf35.DragTabControl");

            changeLogFile = Path.Combine(changeLogFile, string.Format("changelog-{0}.txt", configuration.Version));
            pClient.GenerateChangeLog(changeLogFile);


        }

        #endregion

        #region Building Steps

        private void Build_Step_1_CleanBinariesFromSourceArchive(Configuration configuration)
        {


            // Get solution file
            string solutionFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            solutionFile = Path.Combine(solutionFile, "RapidMinds");
            solutionFile = Path.Combine(solutionFile, "DragTabControl");
            solutionFile = Path.Combine(solutionFile, "DragTabControlSolution.sln");

            CleanBinariesFromSourceArchive task = new CleanBinariesFromSourceArchive(Environment.ExpandEnvironmentVariables(configuration.DevEnvFile), solutionFile);
            task.Execute();
        }

        private void Build_Step_2_GenerateSerialInformation(Configuration configuration)
        {
            GenerateSerialInformation task = new GenerateSerialInformation();

            task.Execute();

            configuration.SerialInformation = task.NextSerialInformation;


        }

        private void Build_Step_3_SetSerialInformationToSourceFiles(Configuration configuration)
        {
            // .NET 3.5

            // Get assembly file
            string assemblyFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            assemblyFile = Path.Combine(assemblyFile, "RapidMinds");
            assemblyFile = Path.Combine(assemblyFile, "DragTabControl");
            assemblyFile = Path.Combine(assemblyFile, "RapidMinds.Controls.Wpf35.DragTabControl");
            assemblyFile = Path.Combine(assemblyFile, "Properties");
            assemblyFile = Path.Combine(assemblyFile, "AssemblyInfo.cs");

            SetSerialInformationToSourceFiles task = new SetSerialInformationToSourceFiles(configuration.GetFullPath(configuration.SetVersionTool),
                assemblyFile,
                configuration.SerialInformation);

            task.Execute();


            // .NET 4.0
            // Get assembly file
            string assemblyFile4 = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            assemblyFile4 = Path.Combine(assemblyFile4, "RapidMinds");
            assemblyFile4 = Path.Combine(assemblyFile4, "DragTabControl");
            assemblyFile4 = Path.Combine(assemblyFile4, "RapidMinds.Controls.Wpf4.DragTabControl");
            assemblyFile4 = Path.Combine(assemblyFile4, "Properties");
            assemblyFile4 = Path.Combine(assemblyFile4, "AssemblyInfo.cs");

            SetSerialInformationToSourceFiles task4 = new SetSerialInformationToSourceFiles(configuration.GetFullPath(configuration.SetVersionTool),
                assemblyFile4,
                configuration.SerialInformation);

            task4.Execute();


        }

        private void Build_Step_4_SetSerialInformationToLicenseAgreement(Configuration configuration)
        {

            DateTime versionCreation = DragTabControlBuilder.GetSourceCheckoutDate(configuration);

            // Get assembly file
            string licenseAgreementFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            licenseAgreementFile = Path.Combine(licenseAgreementFile, "RapidMinds");
            licenseAgreementFile = Path.Combine(licenseAgreementFile, "DragTabControl");
            licenseAgreementFile = Path.Combine(licenseAgreementFile, "Resources");
            licenseAgreementFile = Path.Combine(licenseAgreementFile, "eula.rtf");

            ModifyLicenceAgreement task = new ModifyLicenceAgreement(licenseAgreementFile,
                configuration.SerialInformation,
                configuration.Version,
                versionCreation);

            task.Execute();
        }

        private void Build_Step_5_ModifyInstallShieldProjectFile(Configuration configuration)
        {
            //ModifyInstallShieldTask task = new ModifyInstallShieldTask();
            //task.Executable = this.Configuration.InstallShieldModifierTool; // @"c:\perforce\Starcounter\Dev\Yellow\BuildSystemCode\InstallshieldMod\bin\Release\InstallshieldMod.exe";


            //// Get project file
            //// c:\tmp\TestBuild\RapidMinds\DragTabControl\Setup Installer\Setup Installer.isl
            string projectFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            projectFile = Path.Combine(projectFile, "RapidMinds");
            projectFile = Path.Combine(projectFile, "DragTabControl");
            projectFile = Path.Combine(projectFile, "Setup Installer");
            projectFile = Path.Combine(projectFile, "Setup Installer.isl");

            ModifyInstallShieldProjectFile task = new ModifyInstallShieldProjectFile(configuration.Version,
                configuration.SerialInformation,
                configuration.GetFullPath( configuration.InstallShieldModifierTool),
                projectFile,
                Environment.ExpandEnvironmentVariables(configuration.SourceArchive),
                "DragTabControl");

            task.Execute();

        }

        private void Build_Step_6_BuildDragTabControlSolution(Configuration configuration)
        {
            // Get solution file
            string solutionFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            solutionFile = Path.Combine(solutionFile, "RapidMinds");
            solutionFile = Path.Combine(solutionFile, "DragTabControl");
            solutionFile = Path.Combine(solutionFile, "DragTabControlSolution.sln");

            BuildDragTabControlSolution task = new BuildDragTabControlSolution(Environment.ExpandEnvironmentVariables(configuration.DevEnvFile), solutionFile);

            task.Execute();
        }

        private void Build_Step_7_RunUnitTests(Configuration configuration)
        {

            if( string.IsNullOrEmpty( configuration.MSTestTool) )
            {
                return;
            }

            string unitTestAssembly = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            unitTestAssembly = Path.Combine(unitTestAssembly, "RapidMinds");
            unitTestAssembly = Path.Combine(unitTestAssembly, "DragTabControl");
            unitTestAssembly = Path.Combine(unitTestAssembly, "DragTabControl_UnitTest");
            unitTestAssembly = Path.Combine(unitTestAssembly, "bin");
            unitTestAssembly = Path.Combine(unitTestAssembly, "Release");
            unitTestAssembly = Path.Combine(unitTestAssembly, "DragTabControl_UnitTest.dll");

            RunUnitTests task = new RunUnitTests((Environment.ExpandEnvironmentVariables(configuration.MSTestTool)), unitTestAssembly);
            task.Execute();
        }

        private void Build_Step_8_GenerateClassDocumentation(Configuration configuration)
        {

            // Check if folder exists
            string docFolder = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());
            docFolder = Path.Combine(docFolder, "RapidMinds");
            docFolder = Path.Combine(docFolder, "DragTabControl");
            docFolder = Path.Combine(docFolder, "Documentation");
            docFolder = Path.Combine(docFolder, "ClassAPI");
            if (Directory.Exists(docFolder))
            {
                return;
            }


            string projectFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());

            projectFile = Path.Combine(projectFile, "RapidMinds");
            projectFile = Path.Combine(projectFile, "DragTabControl");
            projectFile = Path.Combine(projectFile, "Documentation");
            projectFile = Path.Combine(projectFile, "DragTabControl.shfbproj");


            // C:\Windows\Microsoft.NET\Framework\ + v4.0.30319

            //            string msbuildPath = Path.Combine(Environment.ExpandEnvironmentVariables("%FrameworkDir%"), Environment.ExpandEnvironmentVariables("%FrameworkVersion%"));
            //            msbuildPath = Path.Combine(msbuildPath, "msbuild.exe");

            string msbuildPath = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";   // TODO

            BuildClassAPIDocumentation task = new BuildClassAPIDocumentation(configuration.Version, msbuildPath, projectFile, Environment.ExpandEnvironmentVariables(configuration.SourceArchive), this.SolutionName);
            task.Execute();

            // Special fix of the Index.Html file

            string oldIndexHtmlFile = Path.Combine(docFolder, "Index.html");
            string newIndexHtmlFile = Path.Combine(docFolder, "index.html");

            File.Move(oldIndexHtmlFile, newIndexHtmlFile);



        }

        private VersionInfo Build_Step_9_CopyBinariesToBinaryArchive(Configuration configuration)
        {
            // Get Prepare done time ( The time when source was "checked out" )


            //string doneFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());
            //doneFile = doneFile + ".busy";  // The build is "Claimed"

            //TextReader tr = new StreamReader(doneFile);
            //string dateStr = tr.ReadLine();

            //dateStr = dateStr.Trim();
            //char[] charToTrim = { '\n', '\r' };
            //dateStr = dateStr.Trim(charToTrim);

            //DateTime versionCreation = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:dd", CultureInfo.CurrentCulture);
            //tr.Close();



            DateTime versionCreation = DragTabControlBuilder.GetSourceCheckoutDate(configuration);


            CopyBinariesToBinaryArchive task = new CopyBinariesToBinaryArchive(configuration.Version,
                Environment.ExpandEnvironmentVariables(configuration.BinaryArchive),
                Environment.ExpandEnvironmentVariables(configuration.SourceArchive),
                configuration.SerialInformation, versionCreation);

            task.Execute();

            return task.VersionInfo;


        }

        #endregion

        #region Helper


        public static string EncryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the encoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToEncrypt = UTF8.GetBytes(Message);

            // Step 5. Attempt to encrypt the string
            try
            {
                ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                Results = Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the encrypted string as a base64 encoded string
            return Convert.ToBase64String(Results);
        }

        public static string DecryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the decoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToDecrypt = Convert.FromBase64String(Message);

            // Step 5. Attempt to decrypt the string
            try
            {
                ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                Results = Decryptor.TransformFinalBlock(DataToDecrypt, 0, DataToDecrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the decrypted string in UTF8 format
            return UTF8.GetString(Results);
        }

        public static DateTime GetSourceCheckoutDate(Configuration configuration)
        {

            string doneFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());
            doneFile = doneFile + ".ok";  // The build is "Not Claimed"

            if (!File.Exists(doneFile))
            {
                doneFile = Path.Combine(Environment.ExpandEnvironmentVariables(configuration.SourceArchive), configuration.Version.ToString());
                doneFile = doneFile + ".busy";  // The build is "Claimed"
            }

            try
            {
                using (TextReader tr = new StreamReader(doneFile))
                {
                    string dateStr = tr.ReadLine();
                    tr.Close();

                    dateStr = dateStr.Trim();
                    char[] charToTrim = { '\n', '\r' };
                    dateStr = dateStr.Trim(charToTrim);

                    DateTime versionCreation = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:dd", CultureInfo.CurrentCulture);
                    return versionCreation;
                }
            }
            catch (Exception)
            {
                return DateTime.Now;
            }


        }
        #endregion



    }




}
