﻿// ***********************************************************************
// <copyright file="PublicModelProvider.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System.Threading;

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

        /// <inheritdoc />
        public CommandInfo Execute(ServerCommand command) {
            command.GetReadyToEnqueue();
            return this.engine.Dispatcher.Enqueue(command);
        }

        /// <inheritdoc />
        public CommandInfo Wait(CommandInfo info) {
            if (info.IsCompleted)
                return info;

            var waitableReference = info.Waitable;
            if (waitableReference == null) {
                // The command either doesn't support waiting using
                // a waitable construct, or it has completed and the
                // public model has been updated accordingly. In any
                // case, we pass it on to the polling-based waiting
                // to either wait using that, or have that return the
                // completed command.
                return Wait(info.Id);
            }
            else if (waitableReference.IsAlive) {
                var waitable = waitableReference.Target;
                if (waitable != null) {
                    WaitHandle waitableHandle = waitable as WaitHandle;
                    try {
                        if (waitableHandle == null) {
                            ManualResetEventSlim slimEvent = waitable as ManualResetEventSlim;
                            slimEvent.Wait();
                        } else {
                            waitableHandle.WaitOne();
                        }
                    } catch (ObjectDisposedException) {
                        // Ignore this. If the server has decided the underlying
                        // construct was ready to be disposed, thats a sure sign
                        // the command has completed.
                    }
                }
            }

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
        public DatabaseInfo[] GetDatabases() {
            lock (databases) {
                DatabaseInfo[] copy = new DatabaseInfo[databases.Values.Count];
                databases.Values.CopyTo(copy, 0);
                return copy;
            }
        }
    }
}
