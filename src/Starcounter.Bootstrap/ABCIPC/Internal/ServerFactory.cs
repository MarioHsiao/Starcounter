
namespace Starcounter.ABCIPC.Internal {

    /// <summary>
    /// Exposes a set of factory methods for creation of servers.
    /// </summary>
    public static class ServerFactory {

        /// <summary>
        /// Creates a server based on named pipes, exposed to clients
        /// using the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">The name of the servers pipe.</param>
        /// <returns>A <see cref="Server"/> that reads requests from, and
        /// writes replies to, a named pipe.</returns>
        public static Server CreateUsingNamedPipes(string pipeName) {
            return new InternalNamedPipeServer(pipeName);
        }
    }
}
