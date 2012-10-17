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
        /// The name of the pipe we use.
        /// </summary>
        const string PipeName = "sc/apps/server";

        /// <summary>
        /// Signature of the delegate that receives notifications when an
        /// executable request to start.
        /// </summary>
        /// <param name="startProperties"></param>
        /// <returns></returns>
        public delegate bool StartRequestHandler(Dictionary<string, string> startProperties);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        /// <summary>
        /// The principal pre-entrypoint call required by all App executables
        /// before the execution of their Main entrypoint. Assures that the
        /// App really lives in a database worker process and transfers control
        /// to such (via the server) if it doesn't.
        /// </summary>
        public static void AssertInDatabaseOrSendStartRequest() {
            var process = Process.GetCurrentProcess();
            if (IsDatabaseWorkerProcess(process))
                return;

            SendStartRequest(CreateStartInfoProperties());
            Environment.Exit(0);
        }

        /// <summary>
        /// Method to be used by server-like processes, interested in getting
        /// notifications when App executable processes requests to start.
        /// </summary>
        /// <param name="handler">A delegate that will be invoked as soon as
        /// a request comes in.</param>
        public static void WaitForStartRequests(StartRequestHandler handler) {
            (new Thread(() => { RunWaitForStartRequestThread(handler); })).Start();
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
        /// meant for the App Main entrypoint once loaded into Starcounter.
        /// </param>
        public static void ParseArguments(string[] allArguments, out string[] toStarcounter, out string[] toAppMain) {
            List<string> starcounter;
            List<string> appMain;

            starcounter = new List<string>();
            appMain = new List<string>();

            foreach (var item in allArguments) {
                if (item.StartsWith("@@")) {
                    starcounter.Add(item.Substring(2));
                } else {
                    appMain.Add(item);
                }
            }

            toStarcounter = starcounter.ToArray();
            toAppMain = appMain.ToArray();
        }

        static void RunWaitForStartRequestThread(StartRequestHandler handler) {
            NamedPipeServerStream pipe;
            byte[] buffer;

            pipe = new NamedPipeServerStream(AppProcess.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            buffer = new byte[1024];

            while (true) {
                pipe.WaitForConnection();

                MemoryStream buffer2 = new MemoryStream(2048);
                do {
                    int readCount = pipe.Read(buffer, 0, buffer.Length);
                    buffer2.Write(buffer, 0, readCount);

                } while (!pipe.IsMessageComplete);

                pipe.Disconnect();

                Dictionary<string, string> properties = KeyValueBinary.ToDictionary(buffer2.ToArray());
                if (!handler(properties))
                    break;
            }
        }

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

        static void SendStartRequest(Dictionary<string, string> properties) {
            string serverName;
            string responseMessage = string.Empty;
            bool result;

            serverName = string.Format("sc//{0}/personal", Environment.MachineName).ToLowerInvariant();

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

        static bool IsDatabaseWorkerProcess(Process p) {
            return p.ProcessName.Equals("boot");
        }
    }
}
