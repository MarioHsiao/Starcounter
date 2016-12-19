using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Starcounter.TestFramework
{
    public class TestLogger
    {
        public const String MappedBuildServerFTPDrive = @"\\scbuildserver\ftp";

        static Boolean _turnOffStatistics = false;

        /// <summary>
        /// Gets/Sets the statistics flag.
        /// </summary>
        public static Boolean TurnOffStatistics
        {
            get { return _turnOffStatistics; }
            set { _turnOffStatistics = value; }
        }
        
        // Object used for exclusive access for statistics reporting.
        static Object LockingObject = new Object();

        /// <summary>
        /// General function used to report statistics.
        /// </summary>
        public static void ReportStatistics(String valueName, Double value)
        {
            // We never report statistics for personal builds.
            if (_turnOffStatistics)
                return;

            lock (LockingObject) {
                Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']", valueName, value.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Returns true if its a nightly build.
        /// </summary>
        static Nullable<Boolean> _nightlyBuild = null;
        public static Boolean IsNightlyBuild()
        {
            if (_nightlyBuild != null)
                return _nightlyBuild.Value;

            // Getting nightly build environment variable.
            String isNightlyBuild = Environment.GetEnvironmentVariable("SC_NIGHTLY_BUILD");

            _nightlyBuild = false;

            // Checking if variable is set to true.
            if ((null != isNightlyBuild) && (0 == String.Compare(isNightlyBuild, "true", true)))
                _nightlyBuild = true;

            return _nightlyBuild.Value;
        }

        /// <summary>
        /// Returns True if its a Release build.
        /// </summary>
        static Nullable<Boolean> _isReleaseBuild = null;
        public static Boolean IsReleaseBuild() {
            if (_isReleaseBuild != null)
                return _isReleaseBuild.Value;

            _isReleaseBuild = (String.Compare(Environment.GetEnvironmentVariable("Configuration"), "Release", true) == 0);

            return _isReleaseBuild.Value;
        }

        /// <summary>
        /// Returns True if its a personal build.
        /// </summary>
        static Nullable<Boolean> _personalBuild = null;
        public static Boolean IsPersonalBuild()
        {
            if (_personalBuild != null)
                return _personalBuild.Value;

            String isPersonalBuild = Environment.GetEnvironmentVariable("BUILD_IS_PERSONAL");

            if ((null != isPersonalBuild) && (0 == String.Compare(isPersonalBuild, "true", true)))
            {
                _personalBuild = true;
                return true;
            }

            _personalBuild = false;
            return false;
        }

        /// <summary>
        /// Returns the build number.
        /// </summary>
        static String _buildNumber = null;
        public static String GetBuildNumber() {
            if (null != _buildNumber)
                return _buildNumber;

            _buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (null == _buildNumber) {
                _buildNumber = "nobuildnum";
            }

            return _buildNumber;
        }

        /// <summary>
        /// Returns True if the run is on the build server.
        /// </summary>
        static Nullable<Boolean> _runningOnBuildServer = null;
        public static Boolean IsRunningOnBuildServer()
        {
            if (_runningOnBuildServer != null)
                return _runningOnBuildServer.Value;

            String isOnBuildServer = Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER");

            if ((null != isOnBuildServer) && (0 == String.Compare(isOnBuildServer, "true", true)))
            {
                _runningOnBuildServer = true;
                return true;
            }

            _runningOnBuildServer = false;
            return false;
        }

        /// <summary>
        /// Skips in-process tests on demand.
        /// </summary>
        static Nullable<Boolean> _skipInprocess = null;
        public static Boolean SkipInProcessTests()
        {
            if (_skipInprocess != null)
                return _skipInprocess.Value;
            
            String skipInprocess = Environment.GetEnvironmentVariable("SC_SKIP_INPROCESS_TESTS");
            if ((null != skipInprocess) && (0 == String.Compare(skipInprocess, "true", true)))
            {
                _skipInprocess = true;
                return true;
            }

            _skipInprocess = false;
            return false;
        }
    }
}