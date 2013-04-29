// ***********************************************************************
// <copyright file="Database.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Configuration;
using Starcounter.Server.PublicModel;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Server {

    /// <summary>
    /// A database maintained by a certain server (represented by
    /// the <see cref="Database.Server"/> property).
    /// </summary>
    internal sealed class Database {
        
        /// <summary>
        /// The server to which this database belongs.
        /// </summary>
        internal readonly ServerEngine Server;

        /// <summary>
        /// The configuration of this <see cref="Database"/>.
        /// </summary>
        internal readonly DatabaseConfiguration Configuration;

        /// <summary>
        /// Gets the simple name of this database.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// Gets the URI of this database.
        /// </summary>
        internal readonly string Uri;

        /// <summary>
        /// Gets or sets the worker process associated with the
        /// current <see cref="Database"/>.
        /// </summary>
        internal Process CodeHostProcess {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value representing the exact command-line
        /// arguments string what was used to start the host.
        /// </summary>
        internal string CodeHostArguments {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the database is supposed to
        /// be running or not.
        /// </summary>
        internal bool SupposedToBeStarted {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the counter used to determine when automatic restart
        /// no longer is viable on failure.
        /// </summary>
        internal int BadAutoRestartCount {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the last time an automatic restart
        /// was triggered.
        /// </summary>
        internal DateTime LastAutoRestartTime {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the list of Apps currently known to the
        /// database represented by this instance.
        /// </summary>
        internal List<DatabaseApp> Apps {
            get;
            set;
        }

        /// <summary>
        /// Intializes a <see cref="Database"/>.
        /// </summary>
        /// <param name="server">The server to which the current database belong.</param>
        /// <param name="configuration">The configuration applied.</param>
        internal Database(ServerEngine server, DatabaseConfiguration configuration) {
            this.Server = server;
            this.Configuration = configuration;
            this.Name = this.Configuration.Name;
            this.Uri = ScUri.MakeDatabaseUri(ScUri.GetMachineName(), server.Name, this.Name).ToString();
            this.Apps = new List<DatabaseApp>();
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="Database"/> in the
        /// form of a public model <see cref="DatabaseInfo"/>.
        /// </summary>
        /// <returns>A <see cref="DatabaseInfo"/> representing the current state
        /// of this database.</returns>
        internal DatabaseInfo ToPublicModel() {
            var process = GetRunningCodeHostProcess();
            var info = new DatabaseInfo() {
                CollationFile = null,
                Configuration = this.Configuration.Clone(this.Configuration.ConfigurationFilePath),
                Name = this.Name,
                MaxImageSize = 0,   // TODO: Backlog
                SupportReplication = false,
                TransactionLogSize = 0, // TODO: Backlog
                Uri = this.Uri,
                HostedApps = this.Apps.ConvertAll<AppInfo>(delegate(DatabaseApp app) {
                    return new AppInfo() {
                        ExecutablePath = app.OriginalExecutablePath,
                        WorkingDirectory = app.WorkingDirectory
                    };
                }).ToArray(),
                HostProcessId = process != null ? process.Id : 0,
                CodeHostArguments = process != null ? this.CodeHostArguments : null,
                DatabaseProcessRunning = Server.DatabaseEngine.IsDatabaseProcessRunning(this),
            };
            return info;
        }

        /// <summary>
        /// Gets the worker process associated with the current <see cref="Database"/>
        /// or NULL if not started or it has exited.
        /// </summary>
        /// <returns></returns>
        internal Process GetRunningCodeHostProcess() {
            var p = CodeHostProcess;
            if (p != null) {
                p.Refresh();
                p = p.HasExited ? null : p;
            }
            return p;
        }
    }
}