// ***********************************************************************
// <copyright file="AppProcess.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;

namespace Starcounter.Apps.Bootstrap {

    /// <summary>
    /// Contains a set of methods that implements the process management
    /// of App processes.
    /// </summary>
    public static class AppProcess {

        /// <summary>
        /// Messages the box.
        /// </summary>
        /// <param name="hWnd">The h WND.</param>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Int32.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        /// <summary>
        /// The principal pre-entrypoint call required by all App executables
        /// before the execution of their Main entrypoint. Assures that the
        /// App really lives in a database worker process and transfers control
        /// to such (via the server) if it doesn't.
        /// </summary>
        [DebuggerNonUserCode]
        public static void AssertInDatabaseOrSendStartRequest() {
            var process = Process.GetCurrentProcess();
            if (IsDatabaseWorkerProcess(process))
                return;

            SendStartRequest(CreateStartInfoProperties());
            Environment.Exit(0);
        }

        /// <summary>
        /// Distributes a set of App arguments into two distinct argument sets:
        /// one directed to Starcounter and the other to the App's entrypoint.
        /// </summary>
        /// <param name="allArguments">All arguments, i.e. the arguments we give
        /// to the App executable on the command line when we start it.</param>
        /// <param name="toStarcounter">The subset of arguments that are
        /// considred meant for Starcounter.</param>
        /// <param name="toAppMain">The subset of arguments that are considred
        /// meant for the App Main entrypoint once loaded into Starcounter.</param>
        public static void ParseArguments(string[] allArguments, out string[] toStarcounter, out string[] toAppMain) {
            List<string> starcounter;
            List<string> appMain;

            starcounter = new List<string>();
            appMain = new List<string>();

            if (allArguments != null) {
                foreach (var item in allArguments) {
                    if (item.StartsWith("@@")) {
                        starcounter.Add(item.Substring(2));
                    } else {
                        appMain.Add(item);
                    }
                }
            }

            toStarcounter = starcounter.ToArray();
            toAppMain = appMain.ToArray();
        }

        /// <summary>
        /// Creates the start info properties.
        /// </summary>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        static Dictionary<string, string> CreateStartInfoProperties() {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            string[] args = System.Environment.GetCommandLineArgs();
            string exeFileName = args[0];
            string workingDirectory = Environment.CurrentDirectory;
            KeyValueBinary serializedArgs;

            Trace.Assert(!exeFileName.Equals(string.Empty));

            if (!Path.IsPathRooted(exeFileName)) {
                var setup = AppDomain.CurrentDomain.SetupInformation;
                exeFileName = Path.Combine(setup.ApplicationBase, setup.ApplicationName);
                Trace.Assert(Path.IsPathRooted(exeFileName));
            }

            if (exeFileName.EndsWith(".vshost.exe"))
                exeFileName = exeFileName.Substring(0, exeFileName.Length - 11) + ".exe";

            serializedArgs = KeyValueBinary.FromArray(args, 1);

            properties.Add("AssemblyPath", exeFileName);
            properties.Add("WorkingDir", workingDirectory);
            properties.Add("Args", serializedArgs.Value);

            return properties;
        }

        /// <summary>
        /// Sends the start request.
        /// </summary>
        /// <param name="properties">The properties.</param>
        static void SendStartRequest(Dictionary<string, string> properties) {
            string serverName;
            string responseMessage = string.Empty;
            bool result;

            serverName = ScUriExtensions.MakeLocalServerPipeString(StarcounterEnvironment.ServerNames.PersonalServer.ToLower());

            var client = ClientServerFactory.CreateClientUsingNamedPipes(serverName);
            try {
                result = client.Send("ExecApp", properties, (Reply reply) => {
                    if (reply.IsResponse) {
                        responseMessage = reply.ToString();
                    }
                });
            } catch (TimeoutException) {
                result = false;
                responseMessage = string.Format("Connecting to server \"{0}\" timed out.", serverName);
            } catch (Exception e) {
                result = false;
                responseMessage = e.Message;
            }
            
            if (!result) {
                MessageBox(IntPtr.Zero, responseMessage, string.Format("Start request failed (server: {0})", serverName), 0x00000030);
            }
        }

        /// <summary>
        /// Determines whether [is database worker process] [the specified p].
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns><c>true</c> if [is database worker process] [the specified p]; otherwise, <c>false</c>.</returns>
        static bool IsDatabaseWorkerProcess(Process p) {
            return p.ProcessName.Equals(StarcounterConstants.ProgramNames.ScCode);
        }
    }
}
