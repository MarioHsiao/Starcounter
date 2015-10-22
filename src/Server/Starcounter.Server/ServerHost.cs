
using Starcounter.Advanced.Configuration;
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
            byte* mem = (byte*) BitsAndBytes.Alloc(128);

            ulong hlogs = ConfigureLogging(configuration);
        }

        static unsafe ulong ConfigureLogging(ServerConfiguration c) {
            uint e;

            e = sccorelog.sccorelog_init();
            if (e != 0) throw ErrorCode.ToException(e);

            ulong hlogs;
            e = sccorelog.star_connect_to_logs(ScUri.MakeServerUri(Environment.MachineName, c.Name), c.LogDirectory, null, &hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            LogManager.Setup(hlogs);

            ServerHost.Log = new LogSource("Starcounter.Server.Host");
            return hlogs;
        }
    }
}