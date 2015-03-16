
using System;
using System.Diagnostics;

namespace Starcounter.Server {

    /// <summary>
    /// Provides information about the calling client context.
    /// </summary>
    public sealed class ClientContext {
        static ClientContext() {
            InitCurrent(KnownClientContexts.UnknownContext);
        }

        /// <summary>
        /// The well-known identity of the current context.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// The process ID of the current context.
        /// </summary>
        public readonly int PID;

        /// <summary>
        /// The logical application the current context runs under.
        /// </summary>
        public readonly string Program;

        /// <summary>
        /// The user the current context runs under.
        /// </summary>
        public readonly string User;

        /// <summary>
        /// The machine the current context runs under.
        /// </summary>
        public readonly string Machine;

        /// <summary>
        /// The current context. Set by client applications, via
        /// <see cref="InitCurrent(string)"/>.
        /// </summary>
        public static ClientContext Current { get; private set; }

        private ClientContext(
            string id, int pid, string program, string user = null, string machine = null) {
            Id = id;
            PID = pid;
            Program = program;
            User = user;
            Machine = machine;
        }

        /// <summary>
        /// Initialize the current <see cref="ClientContext"/> and return
        /// a reference to it.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ClientContext InitCurrent(string id) {
            int pid;
            string program, user, machine;
            GatherSafe(out pid, out program, out user, out machine);
            Current = new ClientContext(id, pid, program, user, machine);
            return Current;
        }

        /// <summary>
        /// Creates a string containing the current client context
        /// information, including information about the current client
        /// and the user.
        /// </summary>
        /// <returns>A string representing the current context.</returns>
        public static string GetCurrentContextInfo() {
            // Note:
            // Don't change this format unless also changing the parser
            // method KnownClientContext.FromContextInfo() in Starcounter.Server.
            // Example contexts:
            //  * "star.exe/123, per@per-asus (via star.exe)"
            //  * "VS/999, per@per-asus (via devenv.exe)"
            var c = ClientContext.Current;
            return string.Format("{0}/{1}, {2}@{3} (via {4})", c.Id, c.PID, c.User, c.Machine, c.Program);
        }

        /// <summary>
        /// Parses the given client context information string (previously
        /// created with <see cref="ClientContext.GetCurrentContextInfo"/>
        /// and extracts the information it contains about the host, i.e.
        /// the logical identity and the process ID.
        /// </summary>
        /// <param name="clientContextInfo">The context info to parse.</param>
        /// <param name="id">The logical host identity.</param>
        /// <param name="pid">The host process id.</param>
        public static void ParseHostInfo(string clientContextInfo, out string id, out int pid) {
            id = KnownClientContexts.UnknownContext;
            pid = 0;
            if (!string.IsNullOrWhiteSpace(clientContextInfo)) {
                int index = clientContextInfo.IndexOf(",");
                if (index != -1) {
                    var hostInfo = clientContextInfo.Substring(0, index);
                    var tokens = clientContextInfo.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                    id = tokens[0];
                    pid = int.Parse(tokens[1]);
                }
            }
        }

        static void GatherSafe(out int pid, out string program, out string user, out string machine) {
            pid = 0;
            program = user = machine = "unknown";
            try {
                var p = Process.GetCurrentProcess();
                pid = p.Id;
                program = p.MainModule.ModuleName;
                user = Environment.UserName.ToLowerInvariant();
                machine = Environment.MachineName.ToLowerInvariant();
            } catch { }
        }
    }

    /// <summary>
    /// Enum-like class with constants of known management
    /// client contexts.
    /// </summary>
    public static class KnownClientContexts {
        /// <summary>
        /// Visual Studio context.
        /// </summary>
        public const string VisualStudio = "VS";
        /// <summary>
        /// The star.exe context.
        /// </summary>
        public const string Star = "star.exe";
        /// <summary>
        /// The staradmin.exe context.
        /// </summary>
        public const string StarAdmin = "staradmin.exe";
        /// <summary>
        /// Context of the administration server.
        /// </summary>
        public const string Admin = "Admin";
        /// <summary>
        /// Used when the context is unknown.
        /// </summary>
        public const string UnknownContext = "Unknown";

        /// <summary>
        /// Extracts the known context identifier from the given
        /// contextInfo string (previously produced by 
        /// ClientContext.GetCurrentContextInfo).
        /// </summary>
        /// <param name="contextInfo">The context info string to parse.</param>
        /// <returns>The context identifier</returns>
        public static string ParseFromContextInfo(string contextInfo) {
            string id;
            int pid;
            ClientContext.ParseHostInfo(contextInfo, out id, out pid);
            return id;
        }
    }
}
