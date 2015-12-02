
using Starcounter.Server.Commands;

namespace Starcounter.Server.PublicModel.Commands {

    /// <summary>
    /// A command representing the request to start an executable.
    /// </summary>
    public sealed class StartExecutableCommand : DatabaseCommand {
        string databaseName;

        public static class DefaultProcessor {
            public static int Token {
                get { return StartExecutableCommandProcessor.ProcessorToken; }
            }

            public static class Tasks {
                public const int PrepareExecutableAndFiles = 1;
                public const int RunInCodeHost = 2;
            }
        }

        /// <summary>
        /// The application that is to be started.
        /// </summary>
        public readonly AppInfo Application;

        /// <summary>
        /// Gets or sets the name of the database the specified application
        /// should load into.
        /// </summary>
        public string DatabaseName {
            get { return databaseName; }
            set {
                databaseName = value;
                DatabaseUri = ScUri.MakeDatabaseUri(ScUri.GetMachineName(), this.Engine.Name, this.databaseName);
            }
        }

        /// <summary>
        /// Gets or sets a value dictating if the App being executed should
        /// be considered not containing anything that needs the database services
        /// of Starcounter (i.e. weaving, SQL, etc).
        /// </summary>
        /// <remarks>
        /// This switch will likely be made obsolete in the near future, so
        /// use it only if you are very certain of what you do and why.
        /// </remarks>
        public bool NoDb {
            get;
            set;
        }

        /// <summary>
        /// Instructs the processor of this command to run the
        /// entrypoint of the executable in an asynchronous fashion.
        /// </summary>
        public bool RunEntrypointAsynchronous {
            get;
            set;
        }

        /// <summary>
        /// Instructs the processor of this command to run the
        /// entrypoint of the executable within the scope of a
        /// write transaction.
        /// </summary>
        public bool TransactEntrypoint {
            get;
            set;
        }

        /// <summary>
        /// Initializes an instance of <see cref="StartExecutableCommand"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> where this command
        /// are to execute.</param>
        /// <param name="application">The application whose executable are to be
        /// started.</param>
        /// <param name="databaseName">The database the given application should be
        /// started in.</param>
        public StartExecutableCommand(ServerEngine engine, string databaseName, AppInfo application) 
            : base(engine, null, "Starting {0} in {1}", application.Name, databaseName) {
            this.Application = application;
            this.DatabaseName = databaseName;
        }

        /// <inheritdoc />
        internal override void GetReadyToEnqueue() {
            base.GetReadyToEnqueue();
        }
    }
}