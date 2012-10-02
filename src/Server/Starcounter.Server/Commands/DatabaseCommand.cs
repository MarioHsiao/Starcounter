
using System;

namespace Starcounter.Server.Commands {
    /// <summary>
    /// Server command targeting a specific database on the server on
    /// which it executes.
    /// </summary>
    internal abstract class DatabaseCommand : ServerCommand {
        /// <summary>
        /// Initializes a new <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="database">The URI of the <see cref="Database">database</see>
        /// this commmand targets.</param>
        /// <param name="node">The <see cref="ServerNode"/> processing the current command.</param>
        /// <param name="descriptionFormat">Formatting string of the command description.</param>
        /// <param name="descriptionArgs">Arguments for <paramref name="descriptionFormat"/>.</param>
        protected DatabaseCommand(
            string databaseUri,
            string descriptionFormat,
            params object[] descriptionArgs
        ) :
            this(databaseUri, string.Format(descriptionFormat, descriptionArgs)) {
        }


        /// <summary>
        /// Initializes a new <see cref="DatabaseCommand"/>.
        /// </summary>
        /// <param name="database">The URI of the <see cref="Database">database</see>
        /// this commmand targets.</param>
        /// <param name="node">Node on which the command should be executed.</param>
        /// <param name="description">Human-readable description of the command.</param>
        protected DatabaseCommand(string databaseUri, string description)
            : base(description) {
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
    }
}