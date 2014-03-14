﻿// ***********************************************************************
// <copyright file="StarcounterEnvironment.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;

namespace Starcounter.Internal
{
    /// <summary>
    /// Class StarcounterEnvironment
    /// </summary>
    public static class StarcounterEnvironment
    {
        /// <summary>
        /// Gets the number of schedulers.
        /// </summary>
        [DllImport("coalmine.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        unsafe extern static UInt32 cm3_get_cpun(void* h_opt, Byte* pcpun);

        [DllImport("coalmine.dll", CallingConvention = CallingConvention.StdCall)]
        extern static UInt32 cm3_eautodet(IntPtr h_opt);

        /// <summary>
        /// Name of the database.
        /// </summary>
        public static string DatabaseNameLower { get; internal set; }

        /// <summary>
        /// Name of the application.
        /// </summary>
        [ThreadStatic]
        public static string AppName;

        /// <summary>
        /// Name of the application.
        /// </summary>
        [ThreadStatic]
        internal static string OrigMapperCallerAppName;

        /// <summary>
        /// Obtains current scheduler id.
        /// </summary>
        public static Byte CurrentSchedulerId
        {
            get
            {
                unsafe
                {
                    Byte cpun = 0;
                    UInt32 errCode = cm3_get_cpun(null, &cpun);
                    if (errCode != 0) {
                        cm3_eautodet(IntPtr.Zero);
                        errCode = cm3_get_cpun(null, &cpun);
                        if (errCode != 0) {
                            return 0;
                        }
                    }
                    return cpun;
                }
            }
        }

        /// <summary>
        /// Gets the number of schedulers.
        /// </summary>
        [DllImport("coalmine.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        unsafe extern static UInt32 cm3_get_cpuc(void* h_opt, Byte* pcpuc);

        static Byte schedulerCount_ = 0;

        /// <summary>
        /// Gets the number of schedulers.
        /// </summary>
        public static Byte SchedulerCount
        {
            get
            {
                if (0 == schedulerCount_)
                {
                    unsafe
                    {
                        Byte cpuc = 0;
                        cm3_get_cpuc(null, &cpuc);
                        schedulerCount_ = cpuc;
                    }
                }

                return schedulerCount_;
            }
        }

        internal static Nullable<Boolean> isCodeHosted = null;

        /// <summary>
        /// Returns if current code is hosted in Starcounter
        /// (i.e. runs in sccode.exe instance).
        /// </summary>
        public static Boolean IsCodeHosted
        {
            get
            {
                if (null != isCodeHosted)
                    return isCodeHosted.Value;

                var name = Process.GetCurrentProcess().ProcessName;
                var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
                isCodeHosted =
                    name.Equals(StarcounterConstants.ProgramNames.ScCode, ignoreCase) ||
                    name.Equals(StarcounterConstants.ProgramNames.ScAdminServer, ignoreCase);

                return isCodeHosted.Value;
            }
        }

        /// <summary>
        /// Checks if given application name is legal.
        /// </summary>
        /// <param name="appName">Application name to test.</param>
        /// <returns>True is name is allowed.</returns>
        public static Boolean IsApplicationNameLegal(String appName)
        {
            // Checking if application name consists only of letters, numbers and underscore.
            if (Regex.IsMatch(appName, @"^[\w]+$"))
                return true;

            return false;
        }

        /// <summary>
        /// Is this application a Starcounter Administrator?
        /// </summary>
        public static Boolean IsAdministratorApp = false;

        /// <summary>
        /// Gateway configuration.
        /// </summary>
        public static class Gateway
        {
            /// <summary>
            /// Number of gateway workers.
            /// </summary>
            public static Byte NumberOfWorkers { get; internal set; }
        }

        /// <summary>
        /// Default configuration parameters.
        /// </summary>
        public static class Default
        {
            /// <summary>
            /// User HTTP port.
            /// </summary>
            public static UInt16 UserHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;

            /// <summary>
            /// System HTTP port.
            /// </summary>
            public static UInt16 SystemHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;

            /// <summary>
            /// Aggregation port.
            /// </summary>
            public static UInt16 AggregationPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerAggregationPort;

            /// <summary>
            /// Default sessions timeout.
            /// </summary>
            public static UInt32 SessionTimeoutMinutes = StarcounterConstants.NetworkPorts.DefaultSessionTimeoutMinutes;
        }

        /// <summary>
        /// Gets a value that holds the path of the Starcounter installation
        /// directory.
        /// </summary>
        public static string InstallationDirectory {
            get {
                var path = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
                if (string.IsNullOrEmpty(path)) {
                    throw ErrorCode.ToException(Error.SCERRBINDIRENVNOTFOUND);
                }
                return path;
            }
        }

        /// <summary>
        /// Assigns the Starcounter installation directory for the current process
        /// based on the calling assembly. Designed to be invoked first/early in
        /// any of our managed bootstrappers (e.g. the code host, the CLI tools,
        /// the weaver).
        /// </summary>
        public static void SetInstallationDirectoryFromEntryAssembly() {
            var assembly = Assembly.GetCallingAssembly();
            Environment.SetEnvironmentVariable(
                StarcounterEnvironment.VariableNames.InstallationDirectory,
                Path.GetDirectoryName(assembly.Location)
                );
        }

        /// <summary>
        /// Default network ports that are used by different Starcounter components.
        /// </summary>
        public static class DefaultPorts
        {
            /// <summary>
            /// Maximum size of ports range that can be used by each server.
            /// </summary>
            public const int ServerPortRangeSize = 128;

            /// <summary>
            /// Default port for the database process.
            /// </summary>
            public const int Database = 10500;

            /// <summary>
            /// Default port for the Prolog SQL parsing process.
            /// </summary>
            public const int SQLProlog = 8066;

            /// <summary>
            /// Default port for the activity monitor server.
            /// </summary>
            public const int ActivityMonitor = 9021;
        }

        /// <summary>
        /// Class SpecialVariables
        /// </summary>
        public static class SpecialVariables
        {
            /// <summary>
            /// Scs the conn max hits per page.
            /// </summary>
            /// <returns>System.UInt32.</returns>
            /// <exception cref="System.NotImplementedException"></exception>
            public static uint ScConnMaxHitsPerPage()
            {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Environment variable names.
        /// </summary>
        public static class VariableNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the environment variable containing the path to the
            /// directory where Starcounter is installed.
            /// </summary>
            public const string InstallationDirectory = "StarcounterBin";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the environment variable containing the name of the
            /// default server on the current machine.
            /// </summary>
            public const string DefaultServer = "StarcounterServer";

            /// <summary>
            /// Provides the name of the environment variable key used to
            /// store the default personal server port for processes, users
            /// and/or machines.
            /// </summary>
            public const string DefaultServerPersonalPort = "StarcounterServerPersonalPort";

            /// <summary>
            /// Provides the name of the environment variable key used to
            /// store the default system server port for processes, users
            /// and/or machines.
            /// </summary>
            public const string DefaultServerSystemPort = "StarcounterServerSystemPort";
        }

        /// <summary>
        /// Special directories.
        /// </summary>
        public static class Directories
        {
            /// <summary>
            /// Full path to USER application data directory.
            /// </summary>
            public static readonly String UserAppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                StarcounterConstants.ProgramNames.ProductName);

            /// <summary>
            /// Full path to SYSTEM application data directory.
            /// </summary>
            public static readonly String SystemAppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                StarcounterConstants.ProgramNames.ProductName);

            /// <summary>
            /// Weaver temp sub directory.
            /// </summary>
            public const String WeaverTempSubDirectory = "weaver";

            /// <summary>
            /// 32BitComponents directory.
            /// </summary>
            public const String Bit32Components = "32BitComponents";
        }

        /// <summary>
        /// Well known file names.
        /// </summary>
        public static class FileNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter GUI administrator program error log.
            /// </summary>
            public const string ClientErrorLog = "ClientErrors.log";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter GUI administrator program statistics
            /// collection file.
            /// </summary>
            public const string ClientStats = "Clientdata.dat";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter Server program statistics
            /// collection file.
            /// </summary>
            public const string ServerStats = "Serverdata.dat";

            /// <summary>
            /// Gateway configuration file name.
            /// </summary>
            public const string GatewayConfigFileName = "scnetworkgateway.xml";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter Server/Administrator version information
            /// NOTE: This code is duplicated in the installer
            /// \perforce\Starcounter\Dev\Yellow\Main\CoreComponents.Net\Starcounter.InstallerWPF\MainWindow.xaml.cs
            /// </summary>
            public const string VersionInfoFileName = "VersionInfo.xml";

            /// <summary>
            /// Default collation Filename prefix
            /// Filename example: TurboText_en-GB_2.dll
            ///     Prefix: TurboText
            ///     Culture: en-GB
            ///     Version: 2
            /// </summary>
            public const String CollationFileNamePrefix = "TurboText";
        }

        /// <summary>
        /// Names of Starcounter servers.
        /// </summary>
        public static class ServerNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the reserved
            /// name used for the default system level server created by
            /// the Starcounter install program on machines where this option
            /// is taken advantage of.
            /// </summary>
            /// <remarks>
            /// Custom servers created manually are not allowed to use this
            /// name.
            /// </remarks>
            public const string SystemServer = "System";

            /// <summary>
            /// Default name postfix for the system server
            /// (appended in brackets to displayed name).
            /// </summary>
            public const string SystemServerDisplayName = "System";

            /// <summary>
            /// Default name for the system server service.
            /// </summary>
            public const string SystemServerServiceName = "StarcounterSystemServer";

            /// <summary>
            /// Holds a constant read-only value representing the reserved
            /// name used for the default user level personal server created by
            /// the Starcounter install program on machines where this option
            /// is taken advantage of.
            /// </summary>
            /// <remarks>
            /// Custom servers created manually are not allowed to use this
            /// name.
            /// </remarks>
            public const string PersonalServer = "Personal";

            /// <summary>
            /// Investigates if the given server name is recognized as one of the
            /// standard reserved server names.
            /// </summary>
            /// <param name="serverName">Name to investigate.</param>
            /// <returns>True if the name is the name of a recognized server; false
            /// if not.</returns>
            public static bool IsRecognizedServerName(string serverName)
            {
                StringComparison comparisonMethod;

                if (string.IsNullOrEmpty(serverName))
                    throw new ArgumentNullException("serverName");

                comparisonMethod = StringComparison.InvariantCultureIgnoreCase;
                return
                    serverName.Equals(ServerNames.SystemServer, comparisonMethod) ||
                    serverName.Equals(ServerNames.PersonalServer, comparisonMethod);
            }
        }

        /// <summary>
        /// Class InternetAddresses
        /// </summary>
        public static class InternetAddresses
        {
            /// <summary>
            /// The starcounter web site
            /// </summary>
            public const string StarcounterWebSite = "http://www.starcounter.com";
            /// <summary>
            /// The starcounter forum
            /// </summary>
            public const string StarcounterForum = "http://www.starcounter.com/forum/";
            /// <summary>
            /// The starcounter wiki
            /// </summary>
            public const string StarcounterWiki = "https://github.com/Starcounter/Starcounter/wiki";
            /// <summary>
            /// The administrator start page
            /// </summary>
            public const string AdministratorStartPage = "http://www.starcounter.com/admin/index.php";
        }
    }
}