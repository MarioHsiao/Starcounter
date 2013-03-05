using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Starcounter;
using Starcounter.Logging;
using Starcounter.TestFramework;

namespace SQLTest
{
    public static class TestRunner
    {
        /// <summary>
        /// Path to debug output file to which debug output are written.
        /// </summary>
        static String debugFilePath = null;

        /// <summary>
        /// Path to new input file generated from query results.
        /// </summary>
        static String generatedInputFilePath = null;

        /// <summary>
        /// Path to input file with test queries.
        /// </summary>
        static String inputFilePath = null;

        /// <summary>
        /// Path to output file to which test result and eventual errors are written.
        /// </summary>
        static String outputFilePath = null;

        /// <summary>
        /// The name of the test.
        /// </summary>
        static String testName = null;

        /// <summary>
        /// Indicates if library is started on client.
        /// </summary>
        static Boolean startedOnClient = false;

        /// <summary>
        /// Indicates if debug output should be written to debug output file.
        /// </summary>
        static Boolean debugOutput = false;

        /// <summary>
        /// Indicates if new input file shouldbe generated from query results.
        /// </summary>
        static Boolean generateInput = false;

        /// <summary>
        // Test logging object.
        /// </summary>
        static TestLogger testLogger = null;

        /// <summary>
        /// Server-side log source.
        /// </summary>
        static LogSource logSource = null;

        /// <summary>
        /// List of test queries to be executed in the test.
        /// </summary>
        static List<TestQuery> queryList = null;

        /// <summary>
        /// Number of test queries that failed in the test.
        /// </summary>
        static Int32 nrFailedQueries = 0;

        /// <summary>
        /// Initializes some variables.
        /// </summary>
        /// <param name="name">Name of the test.</param>
        /// <param name="debug">If debug output should be written.</param>
        /// <param name="input">If new input file shuould be generated.</param>
        /// <param name="onClient">If the code was started on client.</param>
        public static void Initialize(String name, String outputPath, Boolean debug, Boolean input, Boolean onClient)
        {
            testName = name;
            debugOutput = debug;
            generateInput = input;
            startedOnClient = onClient;
            inputFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\s\SQLTest\" + testName + "Input.txt";
            generatedInputFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\s\SQLTest\" + testName + "Generated.txt";
            debugFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\s\SQLTest\" + testName + "Debug.txt";
            if (outputPath == null)
                outputFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\s\SQLTest\" + testName + "Output.txt";
            else {
                if (outputPath[outputPath.Length - 1] == '\\')
                    outputFilePath = outputPath + testName + "Output.txt";
                else
                    outputFilePath = outputPath + @"\" + testName + "Output.txt";
            }
            if (!startedOnClient)
                logSource = new LogSource(testName);
            testLogger = new TestLogger(testName, startedOnClient);
        }

        /// <summary>
        /// Logs event both for server-side and client-side execution.
        /// </summary>
        /// <param name="eventString">Text message.</param>
        /// <param name="msgType">Message type.</param>
        public static void LogEvent(String eventString, TestLogger.LogMsgType msgType)
        {
            testLogger.Log(eventString, msgType);

            if (!startedOnClient)
                logSource.LogNotice(eventString);
        }

        /// <summary>
        /// Logs event both for server-side and client-side execution.
        /// </summary>
        /// <param name="eventString">Text message.</param>
        public static void LogEvent(String eventString)
        {
            testLogger.Log(eventString);

            if (!startedOnClient)
                logSource.LogNotice(eventString);
        }

        /// <summary>
        /// Runs the test on server or client.
        /// </summary>
        public static int RunTest()
        {
            // Checking if we need to skip the process.
            if ((!startedOnClient) && (TestLogger.SkipInProcessTests()))
            {
                // Creating file indicating finish of the work.
                testLogger.Log("SqlTestX in-process test is skipped!", TestLogger.LogMsgType.MSG_SUCCESS);

                return 0;
            }

            Int32 counter = -1;
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                LogEvent("Read test input from file: " + inputFilePath);
                stopwatch.Reset();
                stopwatch.Start();
                queryList = InputReader.ReadQueryListFromFile(inputFilePath, startedOnClient, out counter);
                stopwatch.Stop();
                LogEvent(queryList.Count + " out of " + counter + " queries read from input file in [ms]: " + stopwatch.ElapsedMilliseconds);

                LogEvent("Execute queries (first round).");
                stopwatch.Reset();
                stopwatch.Start();
                QueryExecutor.ResultExecuteQueries(queryList, true);
                stopwatch.Stop();
                LogEvent(queryList.Count + " queries executed (first round) in [ms]: " + stopwatch.ElapsedMilliseconds);

                LogEvent("Execute queries (second round).");
                stopwatch.Reset();
                stopwatch.Start();
                QueryExecutor.ResultExecuteQueries(queryList, false);
                stopwatch.Stop();
                LogEvent(queryList.Count + " queries executed (second round) in [ms]: " + stopwatch.ElapsedMilliseconds);

                LogEvent("Evaluate test result.");
                stopwatch.Reset();
                stopwatch.Start();
                nrFailedQueries = EvaluateTestResult(queryList);
                stopwatch.Stop();
                LogEvent(queryList.Count + " query results evaluated in [ms]: " + stopwatch.ElapsedMilliseconds);
                LogEvent("Failed queries: " + nrFailedQueries);

                LogEvent("Write test result to file: " + outputFilePath);
                stopwatch.Reset();
                stopwatch.Start();
                OutputWriter.WriteOutputToFile(outputFilePath, queryList, FileType.Output);
                stopwatch.Stop();
                LogEvent("Test result written to file in [ms]: " + stopwatch.ElapsedMilliseconds);

                if (debugOutput)
                {
                    LogEvent("Write debug output to file: " + debugFilePath);
                    stopwatch.Reset();
                    stopwatch.Start();
                    OutputWriter.WriteOutputToFile(debugFilePath, queryList, FileType.Debug);
                    stopwatch.Stop();
                    LogEvent(queryList.Count + " queries written to debug file in [ms]: " + stopwatch.ElapsedMilliseconds);
                }

                if (generateInput)
                {
                    LogEvent("Generate new input file: " + generatedInputFilePath);
                    stopwatch.Reset();
                    stopwatch.Start();
                    OutputWriter.WriteOutputToFile(generatedInputFilePath, queryList, FileType.Input);
                    stopwatch.Stop();
                    LogEvent(queryList.Count + " queries written to new input file in [ms]: " + stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception exception)
            {
                LogEvent("Exception: " + exception, TestLogger.LogMsgType.MSG_ERROR);
                throw exception;
            }

            // Checking if there are any failed queries.
            if (nrFailedQueries > 0)
            {
                LogEvent(testName + " has finished with errors!", TestLogger.LogMsgType.MSG_ERROR);
                if (File.Exists(outputFilePath))
                    testLogger.Log(File.ReadAllText(outputFilePath));
            }
            else
                LogEvent(testName + " finished successfully!", TestLogger.LogMsgType.MSG_SUCCESS);
            return nrFailedQueries;
        }

        static Int32 EvaluateTestResult(List<TestQuery> queryList)
        {
            Int32 nrFailedQueries = 0;
            for (Int32 i = 0; i < queryList.Count; i++)
            {
                if (!queryList[i].EvaluateResult(startedOnClient))
                    nrFailedQueries++;
            }
            return nrFailedQueries;
        }
    }
}
