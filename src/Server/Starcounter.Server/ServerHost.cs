
using Starcounter.Configuration;
using Starcounter.Internal;
using Starcounter.Logging;
using System;
using System.Runtime.InteropServices;

namespace Starcounter.Server {
    
    /// <summary>
    /// Represents the host, hosting a server engine.
    /// </summary>
    /// <remarks>
    /// The host is currently configured as part of server engine setup
    /// and can not be accessed from hosting programs themselves.
    /// </remarks>
    internal static class ServerHost {
        /// <summary>
        /// References a log source designed to be used by server hosts
        /// that want to log server-related information occuring outside
        /// the actual engine.
        /// </summary>
        internal static LogSource Log;

        /// <summary>
        /// Configures the host using the content as specified in the given
        /// <see cref="ServerConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to turn to when
        /// configuring the host needs a configurable value.</param>
        internal static unsafe void Configure(ServerConfiguration configuration) {
            byte* mem = (byte*)Marshal.AllocHGlobal(128);

            ulong hmenv = ConfigureMemory(configuration, mem);
            mem += 128;

            ulong hlogs = ConfigureLogging(configuration, hmenv);
        }

        static unsafe ulong ConfigureMemory(ServerConfiguration configuration, void* mem128) {
            uint slabs = (64 * 1024 * 1024) / 4096;  // 64 MB
            ulong hmenv = sccorelib.mh4_menv_create(mem128, slabs);
            if (hmenv != 0) return hmenv;
            throw ErrorCode.ToException(Error.SCERROUTOFMEMORY);
        }

        static unsafe ulong ConfigureLogging(ServerConfiguration c, ulong hmenv) {
            uint e;

            e = sccorelog.sccorelog_init(hmenv);
            if (e != 0) throw ErrorCode.ToException(e);

            ulong hlogs;
            e = sccorelog.sccorelog_connect_to_logs(ScUri.MakeServerUri(Environment.MachineName, c.Name), null, &hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            string logDirectory = c.LogDirectory;
            e = sccorelog.sccorelog_bind_logs_to_dir(hlogs, logDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            LogManager.Setup(hlogs);

            ServerHost.Log = new LogSource("Starcounter.Server.Host");
            return hlogs;
        }
    }
}