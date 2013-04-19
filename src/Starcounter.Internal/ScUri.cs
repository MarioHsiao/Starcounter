// ***********************************************************************
// <copyright file="ScUri.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Net;
using Starcounter.Internal;

namespace Starcounter {

    /// <summary>
    /// Encapsulation of a Starcounter URI.
    /// </summary>
    /// <remarks>
    /// The URI scheme can be accessed from <see cref="ScUri.UriScheme"/>. The
    /// scheme-specific part of Starcounter URI's vary depending on what type of
    /// resource the URI references. The least common denominator, found in all
    /// types of Starcounter URI's, is the machine name.
    /// </remarks>
    public sealed class ScUri {
        private readonly ScUriKind kind;
        private readonly string machineName;
        private readonly string serverName;
        private readonly string databaseName;
        private readonly int port = -1;

        /// <summary>
        /// The URI scheme used by Starcounter.
        /// </summary>
        public const string UriScheme = "sc";

        /// <summary>
        /// The URI scheme used by Starcounter followed by the colon required
        /// by  RFC 3986.
        /// </summary>
        public const string UriSchemeWithColon = UriScheme + ":";

        /// <summary>
        /// Initializes a new <see cref="ScUri" />.
        /// </summary>
        /// <param name="kind">Kind of URI.</param>
        /// <param name="machine">Machine.</param>
        /// <param name="port">The port.</param>
        /// <param name="server">Server. Optional</param>
        /// <param name="instance">Instance. Optional.</param>
        /// <exception cref="System.ArgumentNullException">machine</exception>
        /// <exception cref="System.ArgumentException">machine</exception>
        private ScUri(
            ScUriKind kind,
            string machine,
            int port,
            string server,
            string instance) {
            if (string.IsNullOrEmpty(machine)) {
                throw new ArgumentNullException("machine");
            }

            if (machine.Contains(":")) {
                throw new ArgumentException("machine");
            }

            this.machineName = machine;
            this.port = port;
            this.kind = kind;
            this.databaseName = instance;
            this.serverName = server;
        }

        /// <summary>
        /// Gets the machine name.
        /// </summary>
        public string MachineName {
            get {
                return machineName;
            }
        }

        /// <summary>
        /// Gets the host port.
        /// </summary>
        public int Port {
            get {
                return port;
            }
        }

        /// <summary>
        /// Gets the machine URI.
        /// </summary>
        public ScUri MachineUri {
            get {
                return new ScUri(ScUriKind.Machine, machineName, this.Port, null, null);
            }
        }

        /// <summary>
        /// Gets the server name.
        /// </summary>
        public string ServerName {
            get {
                return serverName;
            }
        }

        /// <summary>
        /// Gets the server URI.
        /// </summary>
        public ScUri ServerUri {
            get {
                return new ScUri(ScUriKind.Server, machineName, this.Port, serverName, null);
            }
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string DatabaseName {
            get {
                return databaseName;
            }
        }

        /// <summary>
        /// Gets the instance URI.
        /// </summary>
        public ScUri DatabaseUri {
            get {
                return new ScUri(ScUriKind.Database, machineName, this.Port, serverName, databaseName);
            }
        }

        /// <summary>
        /// Gets the URI kind.
        /// </summary>
        public ScUriKind Kind {
            get {
                return kind;
            }
        }

        /// <summary>
        /// Makes a machine URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <returns>The machine URI.</returns>
        public static ScUri MakeMachineUri(string machineName) {
            return MakeMachineUri(machineName, -1);
        }

        /// <summary>
        /// Makes a machine URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <param name="port">The port.</param>
        /// <returns>The machine URI.</returns>
        /// <exception cref="System.ArgumentNullException">machineName</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">port</exception>
        public static ScUri MakeMachineUri(string machineName, int port) {
            if (String.IsNullOrEmpty(machineName)) {
                throw new ArgumentNullException("machineName");
            }

            if (port > IPEndPoint.MaxPort || (port < IPEndPoint.MinPort && port != -1)) {
                throw new ArgumentOutOfRangeException("port");
            }

            return new ScUri(ScUriKind.Machine, machineName.ToLowerInvariant(), port, null, null);
        }

        /// <summary>
        /// Makes an server URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <param name="server">server name.</param>
        /// <returns>The server URI.</returns>
        public static ScUri MakeServerUri(string machineName, string server) {
            return MakeServerUri(machineName, -1, server);
        }

        /// <summary>
        /// Makes an server URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <param name="port">The port.</param>
        /// <param name="server">server name.</param>
        /// <returns>The server URI.</returns>
        /// <exception cref="System.ArgumentNullException">machineName</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">port</exception>
        public static ScUri MakeServerUri(string machineName, int port, string server) {
            if (String.IsNullOrEmpty(machineName)) {
                throw new ArgumentNullException("machineName");
            }
            if (String.IsNullOrEmpty(server)) {
                throw new ArgumentNullException("server");
            }

            if (port > IPEndPoint.MaxPort || (port < IPEndPoint.MinPort && port != -1)) {
                throw new ArgumentOutOfRangeException("port");
            }

            return new ScUri(ScUriKind.Server, machineName.ToLowerInvariant(), port, server.ToLowerInvariant(), null);
        }

        /// <summary>
        /// Makes an database URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <param name="server">server name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <returns>The instance URI.</returns>
        public static ScUri MakeDatabaseUri(string machineName, string server, string instanceName) {
            return MakeDatabaseUri(machineName, -1, server, instanceName);
        }

        /// <summary>
        /// Makes an instance URI.
        /// </summary>
        /// <param name="machineName">Machine name.</param>
        /// <param name="port">The port.</param>
        /// <param name="server">server name.</param>
        /// <param name="instanceName">Instance name.</param>
        /// <returns>The instance URI.</returns>
        /// <exception cref="System.ArgumentNullException">machineName</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">port</exception>
        public static ScUri MakeDatabaseUri(string machineName, int port, string server, string instanceName) {
            if (String.IsNullOrEmpty(machineName)) {
                throw new ArgumentNullException("machineName");
            }
            if (port > IPEndPoint.MaxPort || (port < IPEndPoint.MinPort && port != -1)) {
                throw new ArgumentOutOfRangeException("port");
            }

            if (String.IsNullOrEmpty(server)) {
                throw new ArgumentNullException("server");
            }
            if (String.IsNullOrEmpty(instanceName)) {
                throw new ArgumentNullException("instanceName");
            }
            return new ScUri(ScUriKind.Database, machineName.ToLowerInvariant(), port, server.ToLowerInvariant(), instanceName.ToLowerInvariant());
        }


        /// <summary>
        /// Parses an URI and throws an exception in case of error.
        /// </summary>
        /// <param name="uri">A Starcounter URI.</param>
        /// <returns>The parsed <paramref name="uri"/>.</returns>
        public static ScUri FromString(string uri) {
            return FromString(uri, true);
        }

        /// <summary>
        /// Parses an URI and specifies whether an exception should be thrown in case of error.
        /// Format
        /// sc://[user:password@]hostname[:port][/servername[/databasename]]
        /// </summary>
        /// <param name="uri">A Starcounter URI.</param>
        /// <param name="throwOnError"><b>true</b> if an exception should be thrown in case of
        /// error, otherwise <b>false</b>.</param>
        /// <returns>The parsed <paramref name="uri"/>.</returns>
        public static ScUri FromString(string uri, bool throwOnError) {
            if (throwOnError && String.IsNullOrEmpty(uri)) {
                throw new ArgumentNullException("uri");
            }

            // Example
            // sc://username:password@hostname:1234/servername/databasename
            try {
                return ScUri.FromUri(new Uri(uri), throwOnError);
            } catch (Exception e) {
                if (throwOnError) {
                    throw e;
                }
            }

            return null;

        }

        /// <summary>
        /// Froms the URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>ScUri.</returns>
        public static ScUri FromUri(Uri uri) {
            return FromUri(uri, true);
        }

        /// <summary>
        /// Froms the URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="throwOnError">if set to <c>true</c> [throw on error].</param>
        /// <returns>ScUri.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">uri;Cannot parse the URI.</exception>
        public static ScUri FromUri(Uri uri, bool throwOnError) {
            if (throwOnError && uri == null) {
                throw new ArgumentNullException("uri");
            }

            try {
                if (ScUri.UriScheme.Equals(uri.Scheme)) {
                    switch (uri.Segments.Length) {
                        case 0:
                            break;
                        case 1:
                            return new ScUri(ScUriKind.Machine, uri.Host, uri.Port, null, null);
                        case 2:
                            return new ScUri(ScUriKind.Server, uri.Host, uri.Port, uri.Segments[1].TrimEnd('/'), null);
                        case 3:
                            return new ScUri(ScUriKind.Database, uri.Host, uri.Port, uri.Segments[1].TrimEnd('/'), uri.Segments[2].TrimEnd('/'));
                    }
                }

                if (throwOnError) {

                    throw new ArgumentOutOfRangeException("uri", uri, "Cannot parse the URI.");
                }

            } catch (Exception e) {
                if (throwOnError) {
                    throw e;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses the given database connection string to a proper
        /// <see cref="ScUri"/>.
        /// </summary>
        /// <param name="connectionString">String to parse</param>
        /// <returns>A <see cref="ScUri"/> representing the database identified
        /// by <paramref name="connectionString"/>.
        /// </returns>
        public static ScUri FromDbConnectionString(string connectionString) {
            return FromDbConnectionString(connectionString, StarcounterEnvironment.ServerNames.PersonalServer);
        }

        /// <summary>
        /// Parses the given database connection string to a proper
        /// <see cref="ScUri"/>.
        /// </summary>
        /// <param name="connectionString">String to parse</param>
        /// <param name="defaultServer">
        /// The default server to use if the connection string does not
        /// include a server name.</param>
        /// <returns>A <see cref="ScUri"/> representing the database identified
        /// by <paramref name="connectionString"/>.
        /// </returns>
        public static ScUri FromDbConnectionString(string connectionString, string defaultServer) {
            return FromDbConnectionString(connectionString, defaultServer, delegate(string serverName) {
                StringComparison comparisonMethod;

                if (string.IsNullOrEmpty(serverName))
                    throw new ArgumentNullException("serverName");

                comparisonMethod = StringComparison.InvariantCultureIgnoreCase;
                return
                    serverName.Equals(StarcounterEnvironment.ServerNames.SystemServer, comparisonMethod) ||
                    serverName.Equals(StarcounterEnvironment.ServerNames.PersonalServer, comparisonMethod);
            });
        }

        /// <summary>
        /// Parses the given database connection string to a proper
        /// <see cref="ScUri"/>.
        /// </summary>
        /// <param name="connectionString">String to parse</param>
        /// <param name="defaultServer">
        /// The default server to use if the connection string does not
        /// include a server name.</param>
        /// <param name="isRecognizedServer">
        /// A callback used by the parser to identify a segment that could
        /// possibly be a server name.
        /// </param>
        /// <returns>A <see cref="ScUri"/> representing the database identified
        /// by <paramref name="connectionString"/>.
        /// </returns>
        public static ScUri FromDbConnectionString(string connectionString, string defaultServer, Func<string, bool> isRecognizedServer) {
            Uri uri;
            string connectionStringWithScScheme;
            string[] pathEntries;
            string serverName;
            string databaseName;

            if (string.IsNullOrEmpty(connectionString)) {
                throw ErrorCode.ToException(
                    Error.SCERRINVALIDCLIENTCONNECTSTRING,
                    string.Format("{0}Connection string: {1}", Environment.NewLine, connectionString == null ? "<null>" : "<empty>"),
                    (msg, ex) => new ArgumentNullException("connectionString", msg)
                    );
            }

            connectionStringWithScScheme = connectionString;
            if (!connectionStringWithScScheme.StartsWith(ScUri.UriSchemeWithColon, StringComparison.InvariantCultureIgnoreCase))
                connectionStringWithScScheme = string.Format("{0}//{1}", ScUri.UriSchemeWithColon, connectionString);

            try {
                uri = new Uri(connectionStringWithScScheme, UriKind.Absolute);

                if (uri.Segments.Length == 1) {
                    // We allow a certain shorttrack: "sc://[databaseName]", if the
                    // caller has supplied a default server. If so, we reformat the
                    // URI as "sc://127.0.0.1/[defaultserver]/[database]".

                    if (string.IsNullOrEmpty(defaultServer)) {
                        throw ErrorCode.ToException(
                            Error.SCERRINVALIDCLIENTCONNECTSTRING,
                            string.Format("{0}Connection string: {1}", Environment.NewLine, connectionString),
                            (msg, ex) => new ArgumentOutOfRangeException("connectionString", msg)
                            );
                    }

                    serverName = defaultServer;
                    uri = new Uri(
                        string.Format("{0}//{1}/{2}/{3}", ScUri.UriSchemeWithColon, "127.0.0.1", serverName, uri.Authority),
                        UriKind.Absolute
                        );
                }

                // Now get the host. Is it loopback, or points to our IPAddress/computer name?
                // If so, we allow it. Else, it is currently only supported if granted by a
                // given callback.

                if (!uri.IsLoopback) {
                    string host = uri.Host.ToLower();

                    // Check the host: is it a special one?

                    if (isRecognizedServer != null && isRecognizedServer(host)) {
                        // We add local host and push everything one step to the right,
                        // like sc://personal/mydatabase -> sc://127.0.0.1/personal/mydatabase.
                        // And then we interpret the path.

                        uri = new Uri(
                            string.Format("{0}//{1}/{2}{3}", ScUri.UriSchemeWithColon, "127.0.0.1", host, uri.AbsolutePath),
                            UriKind.Absolute
                            );
                    } else {
                        // Check if its the name of the current machine? If so, we change
                        // it to a URI with "127.0.0.1".

                        string machineName = ScUri.GetMachineName();

                        if (host.Equals(machineName, StringComparison.InvariantCultureIgnoreCase)) {
                            uri = new Uri(
                                string.Format("{0}//{1}{2}", ScUri.UriSchemeWithColon, "127.0.0.1", uri.AbsolutePath),
                                UriKind.Absolute
                                );
                        }
                    }
                }
            } catch (UriFormatException formatException) {
                // We were unable to parse a proper URI string from the given input
                // string.
                //   Create a wrapper exception around the URI format exception, pointing
                // developers to our wiki page describing what we excpect from the input
                // string. If they still need to access the underlying formatting exception,
                // we provide it as the inner exception.

                throw ErrorCode.ToException(
                    Error.SCERRINVALIDCLIENTCONNECTSTRING,
                    formatException,
                    string.Format("{0}Connection string: {1}", Environment.NewLine, connectionString),
                    (msg, ex) => new ArgumentOutOfRangeException("connectionString", ex, msg)
                    );
            }

            // The URI is parsed properly. Its format is correct according to syntax.
            // Now check that we can accept it; currently we can not accept anything
            // other than loopback/local addresses.

            if (!uri.IsLoopback) {
                // This is a special database exception. It is a constraint we put on what
                // what servers we currently reach, so it has not to do with incorrect
                // parsing/formatting, but rather to the database domain.

                throw ErrorCode.ToException(
                    Error.SCERRINVALIDCLIENTCONNECTSTRING,
                    string.Format("{0}Connection string: {1}", Environment.NewLine, connectionString)
                    );
            }

            // We have assured the address is a local one and we should investigate
            // the content of the path. We allow one or two tokens. If one, we assume
            // "/database"; if two, we assume "/server/database".

            pathEntries = uri.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (pathEntries.Length == 1) {
                serverName = StarcounterEnvironment.ServerNames.PersonalServer;
                databaseName = pathEntries[0];
            } else if (pathEntries.Length == 2) {
                serverName = pathEntries[0];
                databaseName = pathEntries[1];
            } else {
                // The path of the URI contained more tokens than we could interpret.
                // We consider it out of range.

                throw ErrorCode.ToException(
                    Error.SCERRINVALIDCLIENTCONNECTSTRING,
                    string.Format("{0}Connection string: {1}", Environment.NewLine, connectionString),
                    (msg, ex) => new ArgumentOutOfRangeException("connectionString", ex, msg)
                    );
            }

            return uri.IsDefaultPort
                ? ScUri.MakeDatabaseUri(ScUri.GetMachineName(), serverName, databaseName)
                : ScUri.MakeDatabaseUri(ScUri.GetMachineName(), uri.Port, serverName, databaseName);
        }

        /// <inheritdoc />
        public override string ToString() {
            switch (this.kind) {
                case ScUriKind.Server:
                    return String.Format("sc://{0}/{1}", (this.port == -1) ? this.machineName : this.machineName + ":" + this.port, this.serverName);
                case ScUriKind.Database:
                    return String.Format("sc://{0}/{1}/{2}", (this.port == -1) ? this.machineName : this.machineName + ":" + this.port, this.serverName, this.databaseName);
                case ScUriKind.Machine:
                    return String.Format("sc://{0}/", (this.port == -1) ? this.machineName : this.machineName + ":" + this.port);
                default:
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, (String.Format("Invalid ScUriKind: {0}.", this.kind)));
            }
        }

        /// <summary>
        /// Converts a string into an <see cref="ScUri"/>.
        /// </summary>
        /// <param name="uri">The string to convert.</param>
        /// <returns>An <see cref="ScUri"/> corresponding to <paramref name="uri"/>.</returns>
        public static implicit operator ScUri(string uri) {
            return FromString(uri);
        }

        /// <summary>
        /// Converts an <see cref="ScUri"/> into a string.
        /// </summary>
        /// <param name="uri">The <see cref="ScUri"/> to convert.</param>
        /// <returns>A string equivalent to <paramref name="uri"/>.</returns>
        public static implicit operator string(ScUri uri) {
            return uri.ToString();
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            ScUri other = obj as ScUri;
            return other == null
                ? false
                : NotNullEquals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            // Just to get rid of the compiler warning.
            return base.GetHashCode();
        }

        /// <summary>
        /// Equalses the specified URI string.
        /// </summary>
        /// <param name="uriString">The URI string.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Equals(string uriString) {
            ScUri otherUri;

            if (string.IsNullOrEmpty(uriString))
                return false;

            try {
                otherUri = ScUri.FromString(uriString);
            } catch {
                // If parsing fails, we interpret that as if the given
                // string is not equal to this instance.

                return false;
            }

            return NotNullEquals(otherUri);
        }

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Equals(ScUri other) {
            return other == null
                ? false
                : NotNullEquals(other);
        }

        private bool NotNullEquals(ScUri other) {
            bool result;

            if (object.ReferenceEquals(this, other))
                return true;

            if (other.Kind != this.Kind)
                return false;

            switch (this.Kind) {
                case ScUriKind.Machine:
                    result = this.machineName.Equals(other.machineName)
                        && this.port == other.port;
                    break;
                case ScUriKind.Server:
                    result = this.machineName.Equals(other.machineName)
                        && this.port == other.port
                        && this.serverName.Equals(other.serverName);
                    break;
                case ScUriKind.Database:
                    result = this.machineName.Equals(other.machineName)
                        && this.port == other.port
                        && this.serverName.Equals(other.serverName)
                        && this.databaseName.Equals(other.databaseName);
                    break;
                default:
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, ("Unknown URI kind."));
            }

            return result;
        }

        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string GetMachineName() {
            return Dns.GetHostEntry(String.Empty).HostName;
        }
    }

    /// <summary>
    /// Extends the <see cref="ScUri"/> class with a set of utility
    /// methods allowing uri's to be the foundation for other kind of
    /// identifiers, such as named pipe names.
    /// </summary>
    public static class ScUriExtensions {
        static string localMachineName = Environment.MachineName.ToLowerInvariant();
        const string serverPipeTemplate = "sc//{0}/{1}";
        const string databasePipeTemplate = "sc//{0}/{1}-{2}";

        /// <summary>
        /// Creates a local pipe name, based on the Starcounter URI syntax,
        /// for a server with the given name.
        /// </summary>
        /// <param name="serverName">The name of the server we want to create
        /// a local pipe name for.</param>
        /// <returns>A local pipe name for a server with the given name.</returns>
        public static string MakeLocalServerPipeString(string serverName) {
            return string.Format(serverPipeTemplate, localMachineName, serverName.ToLowerInvariant());
        }

        /// <summary>
        /// Creates a local pipe name, based on the Starcounter URI syntax,
        /// for a database on a server with the given names. 
        /// </summary>
        /// <param name="serverName">The name of the server where the database
        /// lives.</param>
        /// <param name="databaseName">The name of the server we want to create
        /// a local pipe name for.</param>
        /// <returns>A local pipe name for a database with the given name,
        /// running under the control of the given server.</returns>
        public static string MakeLocalDatabasePipeString(string serverName, string databaseName) {
            return string.Format(databasePipeTemplate, localMachineName, serverName.ToLowerInvariant(), databaseName.ToLowerInvariant());
        }

        /// <summary>
        /// Converts a <see cref="ScUri"/> to a corresponding local named
        /// pipe name.
        /// </summary>
        /// <param name="uri">The <see cref="ScUri"/> to be converted to
        /// a local named pipe name.</param>
        /// <returns>A local named pipe name corresponding to the resource
        /// as referenced by the given <see cref="ScUri"/>.</returns>
        public static string ToLocalPipeString(this ScUri uri) {
            if (uri.Kind == ScUriKind.Machine)
                throw new ArgumentOutOfRangeException("uri", "Only server- or database URI's are supported.");

            return uri.Kind == ScUriKind.Server
                ? MakeLocalServerPipeString(uri.ServerName)
                : MakeLocalDatabasePipeString(uri.ServerName, uri.DatabaseName);
        }
    }

    /// <summary>
    /// Kinds of Starcounter URI.
    /// </summary>
    public enum ScUriKind {
        /// <summary>
        /// Machine.
        /// </summary>
        Machine,

        /// <summary>
        /// Agent.
        /// </summary>
        Server,

        /// <summary>
        /// Instance.
        /// </summary>
        Database
    }
}