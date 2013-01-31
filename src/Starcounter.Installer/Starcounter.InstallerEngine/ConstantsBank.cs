﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter;
using System.IO;
using Starcounter.Internal;

namespace Starcounter.InstallerEngine
{
    public static class ConstantsBank
    {
        // Sections in settings file.
        internal const String SettingsSection_Install = "Starcounter Installation Settings";

        // Setup settings constants.
        public const String Setting_InstallPersonalServer = "InstallPersonalServer";
        public const String Setting_InstallSystemServer = "InstallSystemServer";

        public const String Setting_InstallVS2010Integration = "InstallVS2010Integration";
        public const String Setting_InstallVS2012Integration = "InstallVS2012Integration";

        public const String Setting_CreatePersonalServerShortcuts = "CreatePersonalServerShortcuts";
        public const String Setting_AddStarcounterToStartMenu = "AddStarcounterToStartMenu";

        public const String Setting_PersonalServerPath = "PersonalServerPath";
        public const String Setting_SystemServerPath = "SystemServerPath";

        public const String Setting_True = "True";

        // Uninstall settings constants.
        public const String SettingsSection_Uninstall = "Starcounter Uninstall Settings";
        public const String Setting_RemovePersonalServer = "RemovePersonalServer";
        public const String Setting_RemoveSystemServer = "RemoveSystemServer";
        public const String Setting_RemoveVS2010Integration = "RemoveVS2010Integration";
        public const String Setting_RemoveVS2012Integration = "RemoveVS2012Integration";

        // Other constants.
        internal const String SCIconFilename = "Sc-Icon.ico";

        public const String ScGlobalSettingsIniName = "SetupSettings.ini";
        public const String ScGUISetupIniName = "GUISetupSettings.ini";

        public static String ScPostSetupFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User), "ScPostSetupTemp.txt");
        public static String ScStartDemosTemp = Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User), "ScStartDemosTemp.txt");
        public const String ScPostSetupFileArgsSeparator = "###";

        internal const String SCDemoName = "SimpleBenchmark";
        internal const String SCDemoDbName = "StarcounterBenchmarkDemoDb";
        internal const String ScSamplesDemosDirName = "SamplesAndDemos";
        internal const String ScLogFileName = "ScSetup.log";
        internal const String SCVersionFileName = "VersionInfo.xml";
        internal const String SCSilentSetupParam = "silent";
        internal const String SCUninstallParam = "uninstall";
        internal const String SCCleanupParam = "cleanup";
        internal const String SCVSSafeImportsKey = "Starcounter.MsBuild";
        internal const string ScExceptionAssistantContentFileName = "StarcounterExceptionAssistantContent.xml";

        // Constants defined in and fetched from the shared framework assembly
        internal static String SCPersonalServerExeName { get { return StarcounterConstants.ProgramNames.ScService + ".exe"; } }
        public static String SCEnvVariableName { get { return StarcounterEnvironment.VariableNames.InstallationDirectory; } }
        public static String SCEnvVariableDefaultServer { get { return StarcounterEnvironment.VariableNames.DefaultServer; } }
        public static String SCPersonalDatabasesName { get { return StarcounterEnvironment.ServerNames.PersonalUser; } }
        public static String SCSystemDatabasesName { get { return StarcounterEnvironment.ServerNames.System; } }

        public const String SCInstallerGUI = "Starcounter-Setup";
        public const String SCInstallerEngine = "InstallerEngine";
        public const String SCProductName = StarcounterConstants.ProgramNames.ProductName;

        internal const String Registry32BitUninstallPath = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        internal const String Registry64BitUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        // Defines the path to the key holding the "installed product"
        // information in the registry representing an installed integration
        // under the current users VS 2010 config (and in this case, the
        // Starcounter key in particular).
        internal const String RegistryVS2010StarcounterInstalledProductKey = @"SOFTWARE\Microsoft\VisualStudio\10.0_Config\InstalledProducts\Starcounter";

        /// <summary>
        /// Gets the path to the Visual Studio 2010 (10.0) installation directory.
        /// </summary>
        internal static string VS2010InstallationDirectory { get { return GetVisualStudioInstallationDirectory("10.0"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2010 (10.0) IDE directory.
        /// </summary>
        internal static string VS2010IDEDirectory { get { return Path.Combine(VS2010InstallationDirectory, @"Common7\IDE"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2010 (10.0) exception assistant content
        /// directory.
        /// </summary>
        /// <remarks>
        /// The path returned here is the path to the English, standard directory, i.e.
        /// the result is not localized.
        /// </remarks>
        internal static string VS2010ExceptionAssistantDirectory { get { return Path.Combine(VS2010IDEDirectory, @"ExceptionAssistantContent\1033"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2010 (10.0) IDE executable file (i.e. "devenv.exe")
        /// </summary>
        internal static string VS2010DevEnvPath { get { return Path.Combine(VS2010IDEDirectory, "devenv.exe"); } }

        // Defines the path to the key holding the "installed product"
        // information in the registry representing an installed integration
        // under the current users VS 2012 config (and in this case, the
        // Starcounter key in particular).
        internal const String RegistryVS2012StarcounterInstalledProductKey = @"SOFTWARE\Microsoft\VisualStudio\11.0_Config\InstalledProducts\Starcounter";

        /// <summary>
        /// Gets the path to the Visual Studio 2012 (11.0) installation directory.
        /// </summary>
        internal static string VS2012InstallationDirectory { get { return GetVisualStudioInstallationDirectory("11.0"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2012 (11.0) IDE directory.
        /// </summary>
        internal static string VS2012IDEDirectory { get { return Path.Combine(VS2012InstallationDirectory, @"Common7\IDE"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2012 (11.0) exception assistant content
        /// directory.
        /// </summary>
        /// <remarks>
        /// The path returned here is the path to the English, standard directory, i.e.
        /// the result is not localized.
        /// </remarks>
        internal static string VS2012ExceptionAssistantDirectory { get { return Path.Combine(VS2012IDEDirectory, @"ExceptionAssistantContent\1033"); } }

        /// <summary>
        /// Gets the path to the Visual Studio 2012 (11.0) IDE executable file (i.e. "devenv.exe")
        /// </summary>
        internal static string VS2012DevEnvPath { get { return Path.Combine(VS2012IDEDirectory, "devenv.exe"); } }

        /// <summary>
        /// Gets the name of the VSIX installer executable file (i.e. currently "VSIXInstaller.exe").
        /// </summary>
        internal const string VSIXInstallerEngineExecutable = "VSIXInstaller.exe";

        /// <summary>
        /// Returns the root path to the Visual Studio installation directory for a
        /// certain version.
        /// </summary>
        /// <param name="version">Version is 10.0 for Visual Studio 2010, and 11.0 for VS 2012.</param>
        /// <returns>Path to the installation directory.</returns>
        static string GetVisualStudioInstallationDirectory(string version)
        {
            return
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                string.Format(@"..\Program Files (x86)\Microsoft Visual Studio {0}", version)
                );
        }
    }
}
