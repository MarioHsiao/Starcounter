using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Starcounter.TestFramework
{
    public class TestLogger
    {
        public enum LogMsgType
        {
            MSG_INFO,
            MSG_ERROR,
            MSG_SUCCESS,
            MSG_PERFORMANCE
        }

        public const String MSG_ERROR = "MSG_ERROR";
        public const String MSG_PERFORMANCE = "MSG_PERFORMANCE";
        public const String MSG_SUCCESS = "MSG_SUCCESS";

        /// <summary>
        /// Name of the test: e.g. LoadAndLatency
        /// </summary>
        String _testName = null;

        /// <summary>
        /// Full path to test log file: e.g. C:\Temp\LoadAndLatency_client.output
        /// </summary>
        String _testLogFileName = null;

        /// <summary>
        /// Constructor. Accepts test name (e.g. LoadAndLatency), client/database indicator.
        /// </summary>
        public TestLogger(String testName, Boolean isClient) :
            this (testName, isClient, 0)
        {
        }

        /// <summary>
        /// Constructor. Accepts test name (e.g. LoadAndLatency), client/database indicator and read-only flag.
        /// </summary>
        public TestLogger(String testName, Boolean isClient, Int32 testProcessId)
        {
            _testName = testName.ToLowerInvariant();

            // Indicating that we open file only for reading.
            Boolean readOnly = true;

            // Checking if its a test itself that uses logger.
            if (testProcessId == 0)
            {
                readOnly = false;

                // Getting current process ID (to have unique file names).
                testProcessId = Process.GetCurrentProcess().Id;
            }

            // Distinguish between client and in-process test.
            String testLogsDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "ScTestLogs");
            if (isClient)
            {
                _testLogFileName = Path.Combine(testLogsDir, _testName + "_client_" + testProcessId + ".output");
            }
            else
            {
                _testLogFileName = Path.Combine(testLogsDir, _testName + "_database_" + testProcessId + ".output");
            }

            // Checking if we need to create paths or delete existing files.
            if (!readOnly)
            {
                // Checking if directory exists.
                if (!Directory.Exists(testLogsDir))
                    Directory.CreateDirectory(testLogsDir);

                // Checking if old log file already there.
                if (File.Exists(_testLogFileName))
                    File.Delete(_testLogFileName);
            }
        }

        Boolean _turnOffStatistics = false;
        /// <summary>
        /// Gets/Sets the statistics flag.
        /// </summary>
        public Boolean TurnOffStatistics
        {
            get { return _turnOffStatistics; }
            set { _turnOffStatistics = value; }
        }

        Boolean _turnOffImportantMessages = false;
        /// <summary>
        /// Gets/Sets the important messages flag.
        /// </summary>
        public Boolean TurnOffImportantMessages
        {
            get { return _turnOffImportantMessages; }
            set { _turnOffImportantMessages = value; }
        }

        /// <summary>
        /// Returns the full path to log file.
        /// </summary>
        public String LogFileName()
        {
            return _testLogFileName;
        }

        /// <summary>
        /// Logs the general information message (with new line).
        /// </summary>
        public void Log(String message)
        {
            Log(message, LogMsgType.MSG_INFO);
        }

        /// <summary>
        /// Logs the message of specific type.
        /// </summary>
        public void Log(String message, Boolean endLine)
        {
            Log(message, LogMsgType.MSG_INFO, endLine);
        }

        /// <summary>
        /// Re-triable File.AppendAllText.
        /// </summary>
        void AppendAllText(String filePath, String text)
        {
            // Some amount of retries.
            for (Int32 i = 0; i < 10; i++)
            {
                try
                {
                    File.AppendAllText(filePath, text);
                    return;
                }
                catch { }

                // Sleeping some time.
                Thread.Sleep(300);
            }

            // Throwing an exception if all attempts failed.
            throw new Exception("After all attempts still unable to write to test output file: " + filePath);
        }

        /// <summary>
        /// Logs the message of specific type (with new line).
        /// </summary>
        public void Log(String message, LogMsgType type)
        {
            Log(message, type, true);
        }

        /// <summary>
        /// Logs the message of specific type.
        /// </summary>
        public void Log(String message, LogMsgType type, Boolean endLine)
        {
            String fullMessage = message;
            switch (type)
            {
                case LogMsgType.MSG_ERROR:
                    fullMessage = MSG_ERROR + ": " + fullMessage;
                    break;

                case LogMsgType.MSG_SUCCESS:
                    fullMessage = MSG_SUCCESS + ": " + fullMessage;
                    break;

                case LogMsgType.MSG_PERFORMANCE:
                    fullMessage = MSG_PERFORMANCE + ": " + fullMessage;
                    break;
            }

            lock (_testName)
            {
                // Adding message to the log file.
                if (endLine)
                    AppendAllText(_testLogFileName, fullMessage + Environment.NewLine);
                else
                    AppendAllText(_testLogFileName, fullMessage);

                // And printing it to console.
                if (endLine)
                {
                    if (_turnOffImportantMessages)
                        Console.WriteLine(fullMessage);
                    else
                        Console.Error.WriteLine(fullMessage);
                }
                else
                {
                    if (_turnOffImportantMessages)
                        Console.Write(fullMessage);
                    else
                        Console.Error.Write(fullMessage);
                }
            }
        }

        // Path to mapped FTP folder.
        const String MappedBuildServerFTP = "Z:"; // Resembles a mapped drive to \\scbuildserver\ftp.

        // Path to output statistics file.
        static readonly String PathToStatisticsFile = MappedBuildServerFTP + "\\SCDev\\BuildSystem\\Logs\\" + System.Environment.MachineName + "\\ScBuildStatistics.txt";

        /// <summary>
        /// General function used to report statistics.
        /// </summary>
        public void ReportStatistics(String valueName, Int32 value)
        {
            // We never report statistics for personal builds.
            if (_turnOffStatistics || IsPersonalBuild() || IsDebugBuild())
                return;

            lock (_testName)
            {
                Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']", valueName, value);

                // Appending to build server statistics log.
                if (IsRunningOnBuildServer())
                {
                    AppendAllText(PathToStatisticsFile,
                        DateTime.Now + "\t" + valueName + "\t" + value + Environment.NewLine);
                }
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
            Boolean nightlyEnvVar = (Environment.GetEnvironmentVariable("SC_NIGHTLY_BUILD") != null);

            // Checking if its already a personal build and not explicit nightly build.
            if (IsPersonalBuild() && (!nightlyEnvVar))
            {
                _nightlyBuild = false;
                return false;
            }

            // Checking if its a scheduled nightly build.
            if (nightlyEnvVar || (DateTime.Now.Hour >= 1) && (DateTime.Now.Hour <= 6))
            {
                _nightlyBuild = true;
                return true;
            }

            _nightlyBuild = false;
            return false;
        }

        /// <summary>
        /// Returns True if its a personal build.
        /// </summary>
        static Nullable<Boolean> _personalBuild = null;
        public static Boolean IsPersonalBuild()
        {
            if (_personalBuild != null)
                return _personalBuild.Value;

            if (Environment.GetEnvironmentVariable("BUILD_IS_PERSONAL") != null)
            {
                _personalBuild = true;
                return true;
            }

            _personalBuild = false;
            return false;
        }

        /// <summary>
        /// Returns True if its a debug build.
        /// </summary>
        static Nullable<Boolean> _debugBuild = null;
        public static Boolean IsDebugBuild()
        {
            if (_debugBuild != null)
                return _debugBuild.Value;

            if (String.Compare(Environment.GetEnvironmentVariable("Configuration"), "Debug", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                _debugBuild = true;
                return true;
            }

            _debugBuild = false;
            return false;
        }

        /// <summary>
        /// Returns True if the run is on the build server.
        /// </summary>
        static Nullable<Boolean> _runningOnBuildServer = null;
        public static Boolean IsRunningOnBuildServer()
        {
            if (_runningOnBuildServer != null)
                return _runningOnBuildServer.Value;

            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") != null)
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
        public static Boolean SkipInProcessTests()
        {
            return (Environment.GetEnvironmentVariable("SC_SKIP_INPROCESS_TESTS") != null);
        }

        /// <summary>
        /// Checks if client/database process has finished.
        /// </summary>
        public Boolean HasFinished(String allLogText)
        {
            if (allLogText.Contains(MSG_ERROR) ||
                allLogText.Contains(MSG_SUCCESS))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns value for needed message (if it exists), otherwise returns null.
        /// </summary>
        public String GetCertainMessage(String allLogText, String messageType)
        {
            if (allLogText.Contains(messageType))
            {
                return allLogText.Substring(messageType.Length + 2 + allLogText.IndexOf(messageType));
            }

            return null;
        }

        /// <summary>
        /// Returns all text from the log file, or null if the file does not exist.
        /// </summary>
        public String GetAllText()
        {
            if (File.Exists(_testLogFileName))
            {
                String allLogText = File.ReadAllText(_testLogFileName);
                return allLogText;
            }
            return null;
        }

        /// <summary>
        /// Deletes the log file if it exists.
        /// </summary>
        public void DeleteLogFile()
        {
            lock (_testName)
            {
                if (File.Exists(_testLogFileName))
                    File.Delete(_testLogFileName);
            }
        }
    }
}