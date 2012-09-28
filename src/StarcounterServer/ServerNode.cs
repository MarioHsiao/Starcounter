
using Starcounter;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Configuration;
using StarcounterServer.PublicModel;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace StarcounterServer {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    internal sealed class ServerNode {

        /// <summary>
        /// Gets the server configuration.
        /// </summary>
        internal readonly ServerConfiguration Configuration;

        /// <summary>
        /// Gets the database default values to be used when creating databases
        /// and values are not explicitly given.
        /// </summary>
        internal readonly DatabaseDefaults DatabaseDefaultValues;

        /// <summary>
        /// Gets the simple name of the server.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// Gets the URI of this server.
        /// </summary>
        internal readonly string Uri;

        /// <summary>
        /// Gets the dictionary with databases maintained by this server,
        /// keyed by their name.
        /// </summary>
        internal Dictionary<string, Database> Databases { get; private set; }

        /// <summary>
        /// Gets the current public model snapshot of the server and
        /// it's databases. The public model is updated in a thread-safe
        /// maner whenever there is a change in any of the internal
        /// domain state objects (modified by command processors).
        /// </summary>
        internal PublicModelProvider CurrentPublicModel { get; private set; }

        /// <summary>
        /// Initializes a <see cref="ServerNode"/>.
        /// </summary>
        /// <param name="configuration"></param>
        internal ServerNode(ServerConfiguration configuration) {
            this.Configuration = configuration;
            this.DatabaseDefaultValues = new DatabaseDefaults();
            this.Name = configuration.Name;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), this.Name);
            this.Databases = new Dictionary<string, Database>();
        }

        internal void Setup() {
            this.DatabaseDefaultValues.Update(this.Configuration);
            SetupDatabases();
            CurrentPublicModel = new PublicModelProvider(this);
        }

        internal void Start() {
            Server ipcServer;

            // Assume for now interactive mode. This code is still just
            // to get up and running. We'll eventually utilize pipes and
            // spawn another thread, etc.
            
            if (!Console.IsInputRedirected) {
                ipcServer = Utils.PromptHelper.CreateServerAttachedToPrompt();
            } else {
                ipcServer = new Server(Console.In.ReadLine, Console.Out.WriteLine);
            }

            // To handle these two requests is the first 2.2 server
            // milestone.
            //   The creation should accept a single parameter (the
            // name) and use only defaults.

            ipcServer.Handle("CreateDatabase", delegate(Request request) {
                request.Respond(false, "Not implemented");
            });

            ipcServer.Handle("GetDatabase", delegate(Request request) {
                request.Respond(false, "Not implemented");
            });

            ipcServer.Receive();
        }

        internal void Stop() {
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="ServerNode"/> in the
        /// form of a public model <see cref="ServerInfo"/>.
        /// </summary>
        /// <returns>A <see cref="ServerInfo"/> representing the current state
        /// of this server.</returns>
        internal ServerInfo ToPublicModel() {
            var info = new ServerInfo() {
                Configuration = this.Configuration.Clone(),
                DefaultMaxImageSize = this.DatabaseDefaultValues.MaxImageSize,
                DefaultTransactionLogSize = this.DatabaseDefaultValues.TransactionLogSize,
                IsMonitoringSupported = false,
                ServerConfigurationPath = this.Configuration.ConfigurationFilePath,
                Uri = this.Uri,
                UserName = WindowsIdentity.GetCurrent().Name,
            };
            return info;
        }

        void SetupDatabases() {
            // Reads all database information from disk, populating the
            // set of domain object databases.
            // TODO:
        }
    }
}
