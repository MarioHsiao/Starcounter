// ***********************************************************************
// <copyright file="PublicModelProvider.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System.Threading;
using Starcounter.Advanced.Configuration;
using Starcounter.Internal;

namespace Starcounter.Server {

    /// <summary>
    /// Provides access to the public model in a thread-safe manner.
    /// </summary>
    internal sealed class PublicModelProvider : IServerRuntime {
        private ServerEngine engine;
        private readonly Dictionary<string, DatabaseInfo> databases;

        /// <summary>
        /// Gets the current snapshot of server information.
        /// </summary>
        internal ServerInfo ServerInfo { get; private set; }

        /// <summary>
        /// Initializes the public model from the given server.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> maintaining
        /// the current model.</param>
        internal PublicModelProvider(ServerEngine engine) {
            this.engine = engine;
            this.databases = new Dictionary<string, DatabaseInfo>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var database in engine.Databases.Values) {
                databases.Add(database.Uri, database.ToPublicModel());
            }

            UpdateServerInfo(engine);
        }

        /// <summary>
        /// Updates the <see cref="ServerInfo"/> of the public model.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> maintaining
        /// the current model.</param>
        internal void UpdateServerInfo(ServerEngine engine) {
            this.ServerInfo = engine.ToPublicModel();
        }

        /// <summary>
        /// Adds a database to the public model.
        /// </summary>
        /// <param name="database">The database whose state
        /// we are adding to the public model.</param>
        /// <returns>The representation as it appears in
        /// the public model.</returns>
        internal DatabaseInfo AddDatabase(Database database) {
            var info = database.ToPublicModel();
            lock (databases) {
                databases.Add(database.Uri, info);
            }

            Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/__internal_api/databases", database.Name, null, (response) => { });    // async

            return info;
        }

        /// <summary>
        /// Updates a database already part of the public model.
        /// </summary>
        /// <param name="database">The database whose state
        /// we are updating in the public model.</param>
        /// <returns>The representation as it appears in
        /// the public model.</returns>
        internal DatabaseInfo UpdateDatabase(Database database) {
            var info = database.ToPublicModel();
            lock (databases) {
                databases[database.Uri] = info;
            }

            Http.PUT("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/__internal_api/databases/" + database.Name, string.Empty, null, (response) => { });   // async

            return info;
        }

        /// <summary>
        /// Removes a database from the public model.
        /// </summary>
        /// <param name="database"></param>
        internal void RemoveDatabase(Database database) {
            bool removed;

            lock (databases) {
                removed = databases.Remove(database.Uri);
            }

            if (!removed)
                throw new ArgumentException(String.Format("Database '{0}' doesn't exist.", database.Uri));

        }

        /// <inheritdoc />
        public CommandDescriptor[] Functionality
        {
            get
            {
                // NOTE: We are exposing the internal array "as is", instead
                // of giving back a clone. But it's so unlikely that this will
                // be exploited in the kind of controlled environment server
                // hosting really is, so let's not lose sleep over that now.
                return engine.Dispatcher.CommandDescriptors;
            }
        }

        /// <inheritdoc />
        public CommandInfo Execute(
            ServerCommand command,
            Predicate<CommandId> cancellationPredicate = null,
            Action<CommandId> completionCallback = null) {
            command.GetReadyToEnqueue();
            return this.engine.Dispatcher.Enqueue(command, cancellationPredicate, null, completionCallback);
        }

        /// <inheritdoc />
        public CommandInfo Wait(CommandInfo info) {
            if (info.IsCompleted)
                return info;

            if (info.CompletedEvent == null) {
                // The command doesn't support waiting using a waitable
                // construct, i.e it was created w/ the EnableWaiting flag
                // set to false. We pass the call on to the polling-based
                // waiting to either wait using that, or have that return
                // the completed command.
                return Wait(info.Id);
            }

            info.CompletedEvent.Wait();
            return this.engine.Dispatcher.GetRecentCommand(info.Id);
        }

        /// <inheritdoc />
        /// <remarks>The implementation of this method is based on
        /// <see cref="System.Threading.Thread.Sleep(int)"/>, which possibly will be changed
        /// to use events in a future versions.</remarks>
        public CommandInfo Wait(CommandId id) {
            CommandInfo cmd;

            while (true) {
                cmd = this.engine.Dispatcher.GetRecentCommand(id);
                if (cmd == null) {
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, "The command is not part of the recent command list.");
                }
                if (cmd.IsCompleted) {
                    break;
                }

                Thread.Sleep(100);
            };

            return cmd;
        }

        /// <inheritdoc />
        public CommandInfo GetCommand(CommandId id) {
            return this.engine.Dispatcher.GetRecentCommand(id);
        }

        /// <inheritdoc />
        public CommandInfo[] GetCommands() {
            return this.engine.Dispatcher.GetRecentCommands();
        }

        /// <inheritdoc />
        public ServerInfo GetServerInfo() {
            return this.ServerInfo;
        }

        /// <inheritdoc />
        public DatabaseInfo GetDatabase(string databaseUri) {
            lock (databases) {
                DatabaseInfo databaseInfo;
                databases.TryGetValue(databaseUri, out databaseInfo);
                return databaseInfo;
            }
        }

        /// <inheritdoc />
        public DatabaseInfo GetDatabaseByName(string databaseName) {
            lock (databases) {
                var comp = StringComparison.InvariantCultureIgnoreCase;
                foreach (var keyvalue in this.databases) {
                    if (keyvalue.Value.Name.Equals(databaseName, comp)) {
                        return keyvalue.Value;
                    }
                }
                return null;
            }
        }

        /// <inheritdoc />
        public DatabaseInfo[] GetDatabases() {
            lock (databases) {
                DatabaseInfo[] copy = new DatabaseInfo[databases.Values.Count];
                databases.Values.CopyTo(copy, 0);
                return copy;
            }
        }

        public DatabaseConfiguration GetDatabaseConfiguration(string databaseName) {

            if (this.engine.Databases.ContainsKey(databaseName)) {
                Database database = this.engine.Databases[databaseName];
                return database.Configuration;
            }
            return null;
        }
    }
}
