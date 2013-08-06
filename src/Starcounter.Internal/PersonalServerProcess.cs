using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Starcounter.Internal {
    // TODO:
    // Can't figure out a good name for this class. Should probably be renamed and moved.
    public static class PersonalServerProcess {
        private const string serverOnlineEventName = "SCCODE_EXE_ADMINISTRATOR";

        /// <summary>
        /// Checks if the personal server is up and running and is available for new requests.
        /// </summary>
        /// <returns></returns>
        public static bool IsOnline() {
            string serviceName = StarcounterConstants.ProgramNames.ScService;
            foreach (var p in Process.GetProcesses()) {
                if (serviceName.Equals(p.ProcessName)) {
                    // We have the a process with the correct name, lets check if it's the system or personal service.
                    if (p.SessionId != 0) {
                        WaitUntilServerIsOnline(p);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Starts the personal server. When the method returns it is assured that the personal server
        /// is running and available for new requests. Exception will be thrown for any failure that 
        /// happens during startup.
        /// </summary>
        /// <remarks>
        /// This method does not check for an existing running server. If the server is already 
        /// running an exception will be thrown.
        /// </remarks>
        public static void Start() {
            string scBin = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            string exePath = Path.Combine(scBin, StarcounterConstants.ProgramNames.ScService) + ".exe";
            
            Process p = Process.Start(exePath);
            WaitUntilServerIsOnline(p);
        }

        /// <summary>
        /// Listens to the online event for the administrator server. If the server is already 
        /// online the method will return immediately. 
        /// </summary>
        /// <param name="serverProcess"></param>
        private static void WaitUntilServerIsOnline(Process serverProcess) {
            int retries = 60;
            int timeout = 1000; // timeout per wait for signal, not total timeout wait.
            bool signaled;
            EventWaitHandle serverOnlineEvent = null;

            while (true) {
                retries--;
                if (retries == 0)
                    throw ErrorCode.ToException(Error.SCERRWAITTIMEOUT);

                if (serverOnlineEvent == null && !EventWaitHandle.TryOpenExisting(serverOnlineEventName, out serverOnlineEvent)) {
                    Thread.Sleep(100);
                } else {
                    signaled = serverOnlineEvent.WaitOne(timeout);
                    if (signaled)
                        break;
                }

                if (serverProcess.HasExited) {
                    uint errorCode = (uint)serverProcess.ExitCode;
                    if (errorCode != 0) {
                        throw ErrorCode.ToException(errorCode);
                    }
                    throw ErrorCode.ToException(Error.SCERRSERVERNOTAVAILABLE);
                }
            }
        }
    }
}
