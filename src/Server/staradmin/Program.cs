
using Starcounter;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace staradmin {
    class Program {
        static void Main(string[] args) {
            try {
                // Checking if called with 'killall' switch.
                if ((args.Length > 0) && (args[0].StartsWith("-killall", StringComparison.InvariantCultureIgnoreCase)))
                {
                    KillAllScProcesses();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                var e = ErrorCode.ToMessage(Error.SCERRNOTIMPLEMENTED);
                Console.WriteLine(e);
            } finally {
                Console.ResetColor();
            }
        }

        /// <summary>
        /// List of Starcounter processes that should be killed.
        /// </summary>
        internal static String[] ScProcessesList = new String[]
        {
            StarcounterConstants.ProgramNames.ScService,
            StarcounterConstants.ProgramNames.ScAdminServer,
            StarcounterConstants.ProgramNames.ScCode,
            StarcounterConstants.ProgramNames.ScData,
            StarcounterConstants.ProgramNames.ScDbLog,
            StarcounterConstants.ProgramNames.ScIpcMonitor,
            StarcounterConstants.ProgramNames.ScNetworkGateway,
            "scnetworkgatewayloopedtest",
            StarcounterConstants.ProgramNames.ScWeaver,
            StarcounterConstants.ProgramNames.ScSqlParser,
            "ServerLogTail"
        };

        /// <summary>
        /// Kills all Starcounter processes and waits for them to shutdown.
        /// </summary>
        static void KillAllScProcesses(Int32 msToWait = 20000)
        {
            foreach (String procName in ScProcessesList)
            {
                Process[] procs = Process.GetProcessesByName(procName);
                foreach (Process proc in procs)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(msToWait);
                        if (!proc.HasExited)
                        {
                            String processCantBeKilled = "Process " + proc.ProcessName + " can not be killed." + Environment.NewLine +
                                "Please shutdown the corresponding application explicitly.";

                            throw new Exception(processCantBeKilled);
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now.TimeOfDay + ": process '" + procName + "' successfully killed!");
                        }
                    }
                    finally { proc.Close(); }
                }
            }
        }
    }
}
