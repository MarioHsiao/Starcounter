// ***********************************************************************
// <copyright file="ToolInvocationHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Starcounter.Server {

    /// <summary>
    /// Utility class exposing a set of methods aiding in the invocation
    /// of external processes ("tools").
    /// </summary>
    internal static class ToolInvocationHelper {
        static LogSource log = ServerLogSources.Processes;

        /// <summary>
        /// Event raised when a tool has completed.
        /// </summary>
        public static event EventHandler ToolCompleted;

        /// <summary>
        /// Invokes an external process and throws a <see cref="ToolInvocationException"/>
        /// if the process does not return with the exit code 0.
        /// </summary>
        /// <param name="processStartInfo">A <see cref="ProcessStartInfo"/> object
        /// where properties <see cref="ProcessStartInfo.FileName"/> and <see cref="ProcessStartInfo.Arguments"/>
        /// are specified. Other properties should not be specified and may be overwritten by this method.</param>
        public static void InvokeTool(ProcessStartInfo processStartInfo) {
            InvokeTool(processStartInfo, true);
        }

        /// <summary>
        /// Starts an external tool, but does not wait for it to exit.
        /// </summary>
        /// <param name="process"></param>
        public static void StartTool(Process process) {
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            try {
                StartProcess(process);
            } catch (Exception e) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, e,
                    string.Format("Cannot start the process '{0}': {1}.", process.StartInfo.FileName, e.Message));
            }
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        /// <summary>
        /// Waits for a <see cref="System.Diagnostics.Process"/> to exit and then
        /// verifies it exited successfully (i.e. with code 0/zero).
        /// </summary>
        /// <param name="process">The <see cref="System.Diagnostics.Process"/> to wait
        /// for and verify.</param>
        /// <remarks>
        /// If the process exits with another exit code, a <see cref="ToolInvocationException"/>
        /// is raised.
        /// </remarks>
        public static void WaitForToolToExit(Process process) {
            WaitForToolToExit(process, 0);
        }

        /// <summary>
        /// Waits for a <see cref="System.Diagnostics.Process"/> to exit and then
        /// verifies it exited with the given exit code.
        /// </summary>
        /// <param name="process">The <see cref="System.Diagnostics.Process"/> to wait
        /// for and verify.</param>
        /// <param name="exitCode">The exit code to verify against.</param>
        /// <remarks>
        /// If the process exits with another exit code, a <see cref="ToolInvocationException"/>
        /// is raised.
        /// </remarks>
        public static void WaitForToolToExit(
            Process process,
            int exitCode) {
            WaitForToolToExit(process, new int[] { exitCode });
        }

        /// <summary>
        /// Waits for a <see cref="System.Diagnostics.Process"/> to exit and then
        /// verifies it exited with a code represented in the given set of codes.
        /// </summary>
        /// <param name="process">The <see cref="System.Diagnostics.Process"/> to wait
        /// for and verify.</param>
        /// <param name="expectedCodes">List of accepted codes.</param>
        /// <returns>The exit code, if the code was in the list.</returns>
        /// <remarks>
        /// If the process exits with another exit code, a <see cref="ToolInvocationException"/>
        /// is raised.
        /// </remarks>
        public static int WaitForToolToExit(
            Process process,
            int[] expectedCodes) {
            if (process.HasExited == false) {
                process.WaitForExit();
            }

            if (ToolCompleted != null)
                ToolCompleted(process, EventArgs.Empty);

            if (expectedCodes.Contains(process.ExitCode)) {
                return process.ExitCode;
            }

            var empty = new List<string>();
            throw new ToolInvocationException(
                new ToolInvocationResult(process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode, empty, empty));
        }

        /// <summary>
        /// Invokes an external process.
        /// </summary>
        /// <param name="processStartInfo">A <see cref="ProcessStartInfo"/> object
        /// where properties <see cref="ProcessStartInfo.FileName"/> and <see cref="ProcessStartInfo.Arguments"/>
        /// are specified. Other properties should not be specified and may be overwritten by this method.</param>
        /// <param name="checkExitCode"><b>true</b> if the exit code should be checked when the process
        /// completes (and exception <see cref="ToolInvocationException"/> should be thrown if different
        /// than zero), otherwise <b>false</b>.</param>
        /// <returns>A <see cref="ToolInvocationResult"/> with the process exit code and its
        /// console output.</returns>
        public static ToolInvocationResult InvokeTool(ProcessStartInfo processStartInfo, bool checkExitCode) {
            processStartInfo.ErrorDialog = false;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;
            string toolName = Path.GetFileNameWithoutExtension(processStartInfo.FileName).ToUpperInvariant();
            Process process = new Process {
                StartInfo = processStartInfo
            };

            var output = new List<string>();
            var error = new List<string>();
            process.ErrorDataReceived += (sender, e) => { error.Add(e.Data); };
            process.OutputDataReceived += (sender, e) => { output.Add(e.Data); };

            try {
                StartProcess(process);
            } catch (Exception e) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, e,
                    string.Format("Cannot start the process '{0}': {1}.", process.StartInfo.FileName, e.Message));
            }

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            if (!process.HasExited) {
                process.WaitForExit();
            }

            if (ToolCompleted != null)
                ToolCompleted(process, EventArgs.Empty);

            ToolInvocationResult result = new ToolInvocationResult(processStartInfo.FileName, processStartInfo.Arguments, process.ExitCode, output, error);
            if (checkExitCode && result.ExitCode != 0) {
                throw new ToolInvocationException(result);
            }

            return result;
        }

        /// <summary>
        /// Creates an informative exit message from a process that has exited,
        /// including some startup information and the error message retreived
        /// when passing the exit code to the shared error code system.
        /// </summary>
        /// <remarks>
        /// The process supplied is assumed to be a Starcounter tool, or any
        /// other process that return codes that can be mapped to errors in the
        /// Starcounter error code system. To create a message from another
        /// process, consult <see cref="FormatExternalExitMessage"/>.
        /// </remarks>
        /// <example>
        /// Example output: "scdata.exe -someParameter => SCERR1234: Some error".
        /// </example>
        /// <param name="toolProcess">
        /// The process whose exit we want the message from.</param>
        /// <returns>A string with information about the process invocation and
        /// the exit code and exit message.</returns>
        public static string FormatExitMessage(Process toolProcess) {
            if (toolProcess == null)
                throw new ArgumentNullException("toolProcess");

            return string.Format("\"\"{0}\" {1}\" => {2}",
                toolProcess.StartInfo.FileName,
                toolProcess.StartInfo.Arguments,
                ErrorCode.ToMessage((uint)toolProcess.ExitCode).ShortMessage
                );
        }

        /// <summary>
        /// Creates an informative exit message from a process that has exited,
        /// including some startup information and the exit code.
        /// </summary>
        /// <remarks>
        /// If the process supplied is a process that return codes that can be
        /// mapped to errors in the Starcounter error code system, please use
        /// <see cref="FormatExitMessage"/> instead.
        /// </remarks>
        /// <example>
        /// Example output: "devenv.exe -someParameter => Exit code: 123".
        /// </example>
        /// <param name="toolProcess">
        /// The process whose exit we want the message from.</param>
        /// <returns>A string with information about the process invocation and
        /// the exit code.</returns>
        public static string FormatExternalExitMessage(Process toolProcess) {
            if (toolProcess == null)
                throw new ArgumentNullException("toolProcess");

            return string.Format("\"\"{0}\" {1}\" => Exit code: {2}",
                toolProcess.StartInfo.FileName,
                toolProcess.StartInfo.Arguments,
                toolProcess.ExitCode
                );
        }

        private static bool StartProcess(Process process) {
            log.Debug("Starting \"\"{0}\" {1}\" at {2}",
                process.StartInfo.FileName,
                process.StartInfo.Arguments,
                DateTime.Now.ToString("hh.mm.ss.fff"));
            return process.Start();
        }

        private static void ReceiveOutput(string toolName, List<string> output, string message) {
            output.Add(message);
        }
    }

    /// <summary>
    /// Result of the <see cref="ToolInvocationHelper.InvokeTool(System.Diagnostics.ProcessStartInfo,bool)"/>
    /// method. Contains principally the process exit code (<see cref="ExitCode"/> property)
    /// and its output (<see cref="GetOutput"/>).
    /// </summary>
    internal sealed class ToolInvocationResult {
        private readonly List<string> standardOutput;
        private readonly List<string> errorOutput;

        /// <summary>
        /// Initializes a new <see cref="ToolInvocationResult"/>.
        /// </summary>
        /// <param name="fileName">Process file name.</param>
        /// <param name="arguments">Process arguments.</param>
        /// <param name="exitCode">Process exit code.</param>
        /// <param name="standardOut">Standard output of the process to the console.</param>
        /// <param name="errorOut">Errot output of the process to the console.</param>
        public ToolInvocationResult(string fileName, string arguments, int exitCode, List<string> standardOut, List<string> errorOut) {
            this.ExitCode = exitCode;
            this.standardOutput = standardOut;
            this.errorOutput = errorOut;
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets the process file name.
        /// </summary>
        public string FileName {
            get;
            private set;
        }

        /// <summary>
        /// Gets the process arguments.
        /// </summary>
        public string Arguments {
            get;
            private set;
        }

        /// <summary>
        /// Gets the process exit code.
        /// </summary>
        public int ExitCode {
            get;
            private set;
        }

        /// <summary>
        /// Gets the output of the process (i.e. what is normally written
        /// to the console).
        /// </summary>
        /// <returns>An array of strings, each element corresponding to one line of output.</returns>
        public string[] GetOutput() {
            var result = new string[standardOutput.Count + errorOutput.Count];
            int index = 0;

            foreach (var item in standardOutput) {
                result[index] = item;
                index++;
            }
            foreach (var item in errorOutput) {
                result[index] = item;
                index++;
            }
            return result;
        }

        /// <summary>
        /// Gets the error output of the process.
        /// </summary>
        /// <returns>An array of strings, each element corresponding to one line of output.</returns>
        public string[] GetErrorOutput() {
            return errorOutput.ToArray();
        }
    }
}