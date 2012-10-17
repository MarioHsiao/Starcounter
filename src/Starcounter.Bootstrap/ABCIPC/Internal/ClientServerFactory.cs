
namespace Starcounter.ABCIPC.Internal {

    /// <summary>
    /// Exposes a set of factory methods for creation of servers.
    /// </summary>
    public static class ClientServerFactory {

        /// <summary>
        /// Creates a server based on named pipes, exposed to clients
        /// using the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">The name of the servers pipe.</param>
        /// <returns>A <see cref="Server"/> that reads requests from, and
        /// writes replies to, a named pipe.</returns>
        public static Server CreateServerUsingNamedPipes(string pipeName) {
            return new InternalNamedPipeServer(pipeName);
        }

        /// <summary>
        /// Creates a server that reads it's input from the console, using
        /// a simple human-readable protocol on top of the underlying ABCIPC
        /// protocol to read requests and write replies.
        /// </summary>
        /// <returns>A <see cref="Server"/> that reads requests from, and
        /// writes replies to, the console.</returns>
        public static Server CreateServerUsingConsole() {
            return InternalConsoleServer.Create();
        }
    }
}