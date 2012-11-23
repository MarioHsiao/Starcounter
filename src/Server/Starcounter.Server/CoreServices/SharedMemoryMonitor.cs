// ***********************************************************************
// <copyright file="SharedMemoryMonitor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#if false // TODO: Remove!
using System;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Server {
    
    /// <summary>
    /// Implements the servers interaction with the shared memory
    /// connection monitor (primarily just starting it).
    /// </summary>
    internal sealed class SharedMemoryMonitor {
        internal const string ExecutableFileName = "scipcmonitor.exe";
        readonly ServerEngine engine;

        /// <summary>
        /// Initializes a new <see cref="SharedMemoryMonitor"/>.
        /// </summary>
        /// <param name="engine"></param>
        internal SharedMemoryMonitor(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the current <see cref="SharedMemoryMonitor"/>.
        /// </summary>
        internal void Setup() {
        }

        /// <summary>
        /// Starts the shared memory connection monitor.
        /// </summary>
        internal void Start() {
            Process monProc = new Process();
            try {
                var installPath = engine.InstallationDirectory;
                var serverName = engine.Name;
                var logDirectory = engine.Configuration.LogDirectory;

                // Creating start parameters
                monProc.StartInfo.FileName = Path.Combine(installPath, SharedMemoryMonitor.ExecutableFileName);
                monProc.StartInfo.UseShellExecute = false;
                monProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                monProc.StartInfo.CreateNoWindow = true;
                monProc.StartInfo.WorkingDirectory = installPath;
                monProc.StartInfo.Arguments = serverName.ToUpperInvariant() + " \"" + logDirectory + "\"";
                monProc.StartInfo.RedirectStandardOutput = true;

                monProc.Start();

                // Grabbing the standard output text.
                string stdOutput = monProc.StandardOutput.ReadLine();

                // Checking correct output.
                if ((stdOutput != null) && (!stdOutput.EndsWith("monitoring", StringComparison.InvariantCultureIgnoreCase))) {
                    throw ErrorCode.ToException(Error.SCERRSTARTSHAREDMEMORYMONITOR,
                        "Shared memory monitor process returned incorrect initialization message. Please consult monitor logs for more information (located in \"" + logDirectory + "\").");
                }

                // Checking if monitor process crashed.
                if (monProc.HasExited) {
                    throw ErrorCode.ToException(Error.SCERRSTARTSHAREDMEMORYMONITOR,
                        "Shared memory monitor process has been crashed unexpectedly. Please consult monitor logs for more information (located in \"" + logDirectory + "\").");
                }
            } finally {
                monProc.Close();
            }
        }
    }
}
#endif
