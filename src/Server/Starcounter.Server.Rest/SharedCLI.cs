
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;

namespace Starcounter.Server {

    /// <summary>
    /// Provides a set of utilities that can be used by applications
    /// and tools that offer a command-line interface to Starcounter.
    /// </summary>
    /// <remarks>
    /// Examples of standard components and tools that will use this
    /// is star.exe, staradmin.exe and the Visual Studio extension,
    /// the later supporting customization when debugging executables
    /// via the Debug | Command Line project property.
    /// </remarks>
    public static class SharedCLI {
        /// <summary>
        /// Provides the server name used when any of the known server
        /// names doesn't apply.
        /// </summary>
        public const string UnresolvedServerName = "N/A";
        /// <summary>
        /// Provides the default admin server host.
        /// </summary>
        public const string DefaultAdminServerHost = "localhost";
        /// <summary>
        /// Provides the name of the default database being used when
        /// none is explicitly given.
        /// </summary>
        public const string DefaultDatabaseName = StarcounterConstants.DefaultDatabaseName;

        /// <summary>
        /// Defines well-known options, offered by most CLI tools.
        /// </summary>
        public static class Option {
            public const string Serverport = "serverport";
            public const string Server = "server";
            public const string ServerHost = "serverhost";
            public const string Db = "database";
            public const string LogSteps = "logsteps";
            public const string NoDb = "nodb";
            public const string NoAutoCreateDb = "noautocreate";
        }

        /// <summary>
        /// Defines and includes the well-known, shared CLI options in
        /// the given <see cref="SyntaxDefinition"/>.
        /// </summary>
        /// <param name="definition">The <see cref="SyntaxDefinition"/>
        /// in which well-known, shared options should be included.</param>
        public static void DefineWellKnownOptions(SyntaxDefinition definition) {
            definition.DefineProperty(
                Option.Serverport,
                "The port of the server to use.",
                OptionAttributes.Default,
                new string[] { "p" }
                );
            definition.DefineProperty(
                Option.Db,
                "The database to use.",
                OptionAttributes.Default,
                new string[] { "d" }
                );
            definition.DefineProperty(
                Option.Server,
                "Sets the name of the server to use."
                );
            definition.DefineProperty(
                Option.ServerHost,
                "Specifies identify of the server host."
                );
            definition.DefineFlag(
                Option.LogSteps,
                "Enables diagnostic logging. When set, Starcounter will produce a set of diagnostic log entries in the log."
                );
            definition.DefineFlag(
                Option.NoDb,
                "Specifies the code host should run the executable without loading any database data."
                );
            definition.DefineFlag(
                Option.NoAutoCreateDb,
                "Specifies that a database can not be automatically created if it doesn't exist."
                );
        }

        /// <summary>
        /// Resolves the admin server host, port and well-known name from a given
        /// set of command-line arguments.
        /// </summary>
        /// <remarks>
        /// For arguments that are not explicitly given, this method uses environment
        /// defaults as a first fallback and finally constants, in case there is no
        /// environment data available.
        /// </remarks>
        /// <param name="args">Command-line arguments, possibly including shared
        /// options.</param>
        /// <param name="host">The host of the admin server.</param>
        /// <param name="port">The admin server port.</param>
        /// <param name="name">The display name of the admin server, e.g. "Personal".
        /// </param>
        public static void ResolveAdminServer(ApplicationArguments args, out string host, out int port,  out string name) {
            string givenPort;
            int personalDefault;
            int systemDefault;

            personalDefault = EnvironmentExtensions.GetEnvironmentInteger(
                StarcounterEnvironment.VariableNames.DefaultServerPersonalPort,
                StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort
                );

            systemDefault = EnvironmentExtensions.GetEnvironmentInteger(
                StarcounterEnvironment.VariableNames.DefaultServerSystemPort,
                StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort
                );

            if (args.TryGetProperty(Option.Serverport, out givenPort)) {
                port = int.Parse(givenPort);

                // If a port is specified, that always have precedence.
                // If it is, we try to pair it with a server name based on
                // the following priorities:
                //   1) Getting a given name on the command-line
                //   2) Trying to pair the port with a default server based
                // on known server port defaults.
                //   3) Finding a server name configured in the environment.
                //   4) Using a const string (e.g. "N/A")

                if (!args.TryGetProperty(Option.Server, out name)) {
                    if (port == personalDefault) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    } else if (port == systemDefault) {
                        name = StarcounterEnvironment.ServerNames.SystemServer;
                    } else if (port == StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    } else if (port == StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort) {
                        name = StarcounterEnvironment.ServerNames.SystemServer;
                    } else {
                        name = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.DefaultServer);
                        if (string.IsNullOrEmpty(name)) {
                            name = UnresolvedServerName;
                        }
                    }
                }
            } else {

                // No port given. See if a server was specified by name and try
                // to figure out a port based on that, or a port based on a server
                // name given in the environment.
                //   If a server name in fact IS specified (and no port is), we
                // must match it against one of the known server names. If it is
                // not part of them, we refuse it.
                //   If no server is specified either on the command line or in the
                // environment, we'll assume personal and the default port for that.

                if (!args.TryGetProperty(Option.Server, out name)) {
                    name = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.DefaultServer);
                    if (string.IsNullOrEmpty(name)) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    }
                }

                var comp = StringComparison.InvariantCultureIgnoreCase;

                if (name.Equals(StarcounterEnvironment.ServerNames.PersonalServer, comp)) {
                    port = personalDefault;
                } else if (name.Equals(StarcounterEnvironment.ServerNames.SystemServer, comp)) {
                    port = systemDefault;
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRUNSPECIFIED,
                        string.Format("Unknown server name: {0}. Please specify the port using '{1}'.",
                        name,
                        Option.Serverport));
                }
            }

            if (!args.TryGetProperty(Option.ServerHost, out host)) {
                host = SharedCLI.DefaultAdminServerHost;
            } else {
                if (host.StartsWith("http", true, null)) {
                    host = host.Substring(4);
                }
                host = host.TrimStart(new char[] { ':', '/' });
            }
        }

        /// <summary>
        /// Resolves the named database to use from a given set of
        /// command-line arguments.
        /// </summary>
        /// <remarks>
        /// If database argument are not explicitly given, this method uses environment
        /// defaults as a first fallback and finally constants, in case there is no
        /// environment data available.
        /// </remarks>
        /// <param name="args">Command-line arguments to consult.</param>
        /// <returns>The name of the database.</returns>
        public static string ResolveDatabase(ApplicationArguments args) {
            string database;
            if (!args.TryGetProperty(Option.Db, out database)) {
                database = SharedCLI.DefaultDatabaseName;
            }
            return database;
        }
    }
}