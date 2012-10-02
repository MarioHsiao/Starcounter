
using System;
using System.Collections.Generic;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

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
            this.databases = new Dictionary<string, DatabaseInfo>();

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
        /// <param name="database"></param>
        internal void AddDatabase(Database database) {
            var info = database.ToPublicModel();
            lock (databases) {
                databases.Add(database.Uri, info);
            }
        }

        /// <summary>
        /// Updates a database already part of the public model.
        /// </summary>
        /// <param name="database"></param>
        internal void UpdateDatabase(Database database) {
            var info = database.ToPublicModel();
            lock (databases) {
                databases[database.Uri] = info;
            }
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

        /// </inheritdoc>
        public CommandInfo Execute(ServerCommand command) {
            return this.engine.Dispatcher.Enqueue(command);
        }

        /// </inheritdoc>
        public CommandInfo GetCommand(CommandId id) {
            return this.engine.Dispatcher.GetRecentCommand(id);
        }

        /// </inheritdoc>
        public CommandInfo[] GetCommands() {
            return this.engine.Dispatcher.GetRecentCommands();
        }

        /// </inheritdoc>
        public ServerInfo GetServerInfo() {
            return this.ServerInfo;
        }

        /// </inheritdoc>
        public DatabaseInfo GetDatabase(string databaseUri) {
            lock (databases) {
                DatabaseInfo databaseInfo;
                databases.TryGetValue(databaseUri, out databaseInfo);
                return databaseInfo;
            }
        }

        /// </inheritdoc>
        public DatabaseInfo[] GetDatabases() {
            lock (databases) {
                DatabaseInfo[] copy = new DatabaseInfo[databases.Values.Count];
                databases.Values.CopyTo(copy, 0);
                return copy;
            }
        }
    }
}
