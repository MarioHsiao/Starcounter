using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace RapidMinds.BuildSystem.Common
{
    [Serializable]
    public class Configuration
    {
        #region Properties

        public string ProjectName { get; set; }

        public string Title { get; set; }

        public string Plugin { get; set; }

        /// <summary>
        /// Gets or sets the root path.
        /// The full path to this configuration file
        /// This is set when file is loaded
        /// </summary>
        /// <value>
        /// The root path.
        /// </value>
        [XmlIgnoreAttribute]
        public string RootPath { get; set; }


        [XmlIgnoreAttribute]
        [Obsolete("TODO: Remove Version from configuration")]
        public Version Version { get; set; }

        [XmlIgnoreAttribute]
        [Obsolete("TODO: Remove Version from configuration")]
        public string SerialInformation { get; set; }


        public string SetVersionTool { get; set; }
        public string DevEnvFile { get; set; }
        public string InstallShieldModifierTool { get; set; }
        public string MSTestTool { get; set; }


        public string PerforceServer { get; set; }
        public string PerforcePort { get; set; }
        public string PerforceUsername { get; set; }
        public string PerforcePassword { get; set; }

        public bool IsSetPerforceLabel { get; set; }
        public bool IsChangeLog { get; set; }
        public bool IsGenerateClassAPI { get; set; }

        /// <summary>
        /// Gets or sets the perforce lable depot.
        /// </summary>
        /// <remarks>
        /// This is used to label files in the depot, it's used when generating the changelog
        /// </remarks>
        /// <value>
        /// The perforce lable depot.
        /// </value>
        public string PerforceLableDepot { get; set; }

        /// <summary>
        /// Gets or sets the root path .
        /// </summary>
        /// <remarks>
        /// This path should point to where the perforce root folder is located (C:\Perforce)
        /// </remarks>
        /// <value>
        /// The root path.
        /// </value>
        //public string PerforceRootPath { get; set; }

        public string BinaryArchive { get; set; }
        public string SourceArchive { get; set; }

        // %RAMDISK%\RapidMinds\SpeedGrid\NightlyBuild\<hear will Speedgrid source be>
        public string CheckoutPath { get; set; }


        public string FTPHost { get; set; }
        public string FTPUserName { get; set; }
        public string FTPPassword { get; set; }
        public string FTPBinaryPath { get; set; }
        //public BuildType BuildType { get; set; }
        //public string Platform { get; set; }
        //public bool Debug { get; set; }
        //public string TargetFrameworkVersion { get; set; }
        //[System.Xml.Serialization.XmlIgnoreAttribute]
        // [System.ComponentModel.DefaultValueAttribute("2002")]

        public string FTPClassDocumentationPath { get; set; }



        [XmlIgnoreAttribute]
        public IBuilder Builder;

        #endregion


        /// <summary>
        /// Loads the specified configuration.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        public static Configuration Load(string file)
        {
            Configuration configuration;
            if (File.Exists(file))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    TextReader tr = new StreamReader(file);
                    configuration = (Configuration)serializer.Deserialize(tr);
                    configuration.RootPath = file;

                    tr.Close();
                    return configuration;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                throw new FileNotFoundException("Can not load configuration", file);
            }
        }


        /// <summary>
        /// Saves the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="file">The file.</param>
        public static void Save(Configuration configuration, string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            TextWriter tw = new StreamWriter(file);
            serializer.Serialize(tw, configuration);
            tw.Close();


        }


        /// <summary>
        /// Sets the default.
        /// </summary>
        public void SetDefault()
        {

            this.Title = "Default configuration";

            this.SetVersionTool = @"%SystemDrive%\perforce\Starcounter\Dev\Yellow\BuildSystemCode\SetAssemblyVersion\bin\Release\SetVersionNumber.exe";
            this.DevEnvFile = @"%VS100COMNTOOLS%\..\IDE\devenv.com";
            this.InstallShieldModifierTool = @"%SystemDrive%\perforce\Starcounter\Dev\Yellow\BuildSystemCode\InstallshieldMod\bin\Release\InstallshieldMod.exe";


            this.SourceArchive = @"%SystemDrive%\tmp\RapidMinds\SpeedGrid\SourceArchive\Nightlybuilds";

            // Artifacts
            this.BinaryArchive = @"%SystemDrive%\tmp\RapidMinds\SpeedGrid\BinArchive\Nightlybuilds";

            this.CheckoutPath = @"%SystemDrive%\perforce\RapidMinds\SpeedGrid";


            this.FTPHost = "ftp://127.0.0.1:21";
            this.FTPUserName = "anonymous";
            this.FTPPassword = "janeDoe@contoso.com";
            this.FTPBinaryPath = @"\www\files\Builds\NightlyBuilds";

            // Perforce
            this.PerforceServer = "sccserver";
            this.PerforcePort = "1668";
            this.PerforceUsername = "andwah";
            this.PerforcePassword = "NNc6Vpf/kyk="; // TODO, this is a crypted password and not used in perforce
            this.PerforceLableDepot = @"//RapidMinds/SpeedGrid/SpeedGrid/...";
            this.IsSetPerforceLabel = false;
            this.IsChangeLog = false;
            this.IsGenerateClassAPI = false;
            this.FTPClassDocumentationPath = @"\www\ClassAPI\NightlyBuilds";
            this.MSTestTool = @"c:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\MSTest.exe";
        }


        // 1) c:\FTP\SCDev\BuildSystem\SetVersionNumber.exe /F "%teamcity.build.checkoutDir%\SpeedGrid\Properties\AssemblyInfo.cs" "%env.BUILD_NUMBER%" /APPEND
        // 2) c:\FTP\SCDev\BuildSystem\InstallshieldMod.exe "%teamcity.build.checkoutDir%\Setup Installer\Setup Installer.isl" "%env.BUILD_NUMBER%" "%env.RAMDISK%\"
        // 3) C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com "%teamcity.build.checkoutDir%\SpeedGridSolution.sln" /build Release
        // 4) MSTest
        //    Path to MSTest.exe:
        //    %system.MSTest.10.0%
        //
        //    Edit assemblies include list: 
        //   "%teamcity.build.checkoutDir%\SpeedGrid_UnitTest\bin\Release\SpeedGrid_UnitTest.dll"



        public string GetFullPath( string path )
        {

            string toolPath = Environment.ExpandEnvironmentVariables(path);

            if (Path.IsPathRooted(toolPath) == true)
            {
                // absolute

                return toolPath;
            }
            else
            {
                // relative
                return Path.Combine(Path.GetDirectoryName(this.RootPath), toolPath);
            }


        }


    }


}

