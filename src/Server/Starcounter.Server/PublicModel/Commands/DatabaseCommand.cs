
using System;

namespace Starcounter.Server.PublicModel.Commands {
    
    /// <summary>
    /// Server command targeting a specific database on the server on
    /// which it executes.
    /// </summary>
    public abstract class DatabaseCommand : ServerCommand {
        /// <summary>
        /// Utility method that creates a database <see cref="ScUri"/> as a string,
        /// referencing the specified database hosted in the given server engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        internal static string CreateDatabaseUri(ServerEngine engine, string databaseName) {
            ScUri serverUri = ScUri.FromString(engine.Uri);
            return ScUri.MakeDatabaseUri(serverUri.MachineName, serverUri.ServerName, databaseName).ToString();
        }

        /// <summary>
        /// Initializes a new <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="database">The URI of the <see cref="Database">database</see>
        /// this commmand targets.</param>
        /// <param name="descriptionFormat">Formatting string of the command description.</param>
        /// <param name="descriptionArgs">Arguments for <paramref name="descriptionFormat"/>.</param>
        protected DatabaseCommand(
            ServerEngine engine,
            string databaseUri,
            string descriptionFormat,
            params object[] descriptionArgs
        ) :
            this(engine, databaseUri, string.Format(descriptionFormat, descriptionArgs)) {
        }


        /// <summary>
        /// Initializes a new <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="database">The URI of the <see cref="Database">database</see>
        /// this commmand targets.</param>
        /// <param name="description">Human-readable description of the command.</param>
        protected DatabaseCommand(ServerEngine engine, string databaseUri, string description)
            : base(engine, description) {
            if (databaseUri == null) {
                throw new ArgumentNullException("databaseUri");
            }
            this.DatabaseUri = databaseUri;
        }

        /// <summary>
        /// Gets the URI of the <see cref="Database">database</see> this
        /// command targets.
        /// </summary>
        /// <remarks>
        /// Most specializations of <see cref="DatabaseCommand"/> will likely want
        /// to keep a strong reference to the server side database object representing
        /// the database. We reject to force referencing the database here though,
        /// since not all commands are sure to either have the reference accesible
        /// or want to keep a strong reference to it. Hence, we stick to keeping the
        /// URI as a string.
        /// </remarks>
        public string DatabaseUri {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of the database this command targets, scoped by the
        /// server engine.
        /// </summary>
        public string Name {
            get {
                return ScUri.FromString(this.DatabaseUri).DatabaseName;
            }
        }
    }
}