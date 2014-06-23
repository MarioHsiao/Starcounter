using Starcounter.CommandLine;

namespace Starcounter.CLI {
    /// <summary>
    /// Represents a reference to a certain admin server, identified
    /// principally by its name. The server reference can be used to
    /// construct <see cref="Starcounter.Node"/> instances.
    /// </summary>
    public sealed class ServerReference {
        /// <summary>
        /// Gets the host the current instance reference.
        /// </summary>
        public readonly string Host;

        /// <summary>
        /// Gets the port the current instance reference.
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// Gets or sets the logical server name of the server
        /// the current instance reference.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Creates a new <see cref="ServerReference"/> from well-known CLI
        /// input given. Uses defaults for everything not given as input.
        /// </summary>
        /// <param name="args">The input to resolve the reference from.</param>
        /// <returns>A server reference based on the given input and/or
        /// defaults, depending on what is given.</returns>
        public static ServerReference CreateFromCommonCLI(ApplicationArguments args) {
            string host;
            int port;
            string name;

            SharedCLI.ResolveAdminServer(args, out host, out port, out name);
            return new ServerReference(host, port) { Name = name };
        }

        /// <summary>
        /// Creates a new <see cref="ServerReference"/> based on configured defaults.
        /// </summary>
        /// <returns>A reference to a server based on defaults.</returns>
        public static ServerReference CreateDefault() {
            return CreateFromCommonCLI(ApplicationArguments.Empty);
        }

        /// <summary>
        /// Initialize a new <see cref="ServerReference"/>.
        /// </summary>
        /// <param name="host">The host of the server referenced.</param>
        /// <param name="port">The port the server communicates over.</param>
        public ServerReference(string host, int port) {
            Host = host;
            Port = port;
        }
    }
}