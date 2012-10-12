﻿
using System;
using System.IO;
using System.Linq;
using Starcounter.Apps.Bootstrap;

namespace Starcounter.Server.PublicModel.Commands {

    /// <summary>
    /// A command representing the request to start an executable.
    /// </summary>
    public sealed class ExecAppCommand : ServerCommand {
        /// <summary>
        /// Gets the path to the assembly file requesting to start.
        /// </summary>
        public readonly string AssemblyPath;

        /// <summary>
        /// Gets the path to the directory the requesting executable
        /// has given as it's working directory;
        /// </summary>
        public readonly string WorkingDirectory;

        /// <summary>
        /// Gets the arguments with which the requesting executable
        /// was started with.
        /// </summary>
        public readonly string[] Arguments;

        /// <summary>
        /// Gets or sets the name of the database the App should load
        /// into.
        /// </summary>
        public string DatabaseName {
            get;
            set;
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
        /// Gets all arguments targeting Starcounter.
        /// </summary>
        internal string[] ArgumentsToStarcounter {
            get;
            private set;
        }

        /// <summary>
        /// Gets all arguments targeting the about-to-be started application.
        /// </summary>
        internal string[] ArgumentsToApplication {
            get;
            private set;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ExecAppCommand"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> where this command
        /// are to execute.</param>
        /// <param name="assemblyPath">Path to the assembly requesting to start.</param>
        /// <param name="workingDirectory">Working directory the executable has requested to run in.</param>
        /// <param name="arguments">Arguments as passed to the requesting executable.</param>
        public ExecAppCommand(ServerEngine engine, string assemblyPath, string workingDirectory, string[] arguments)
            : base(engine, "Starting {0}", Path.GetFileName(assemblyPath)) {
            if (string.IsNullOrEmpty(assemblyPath)) {
                throw new ArgumentNullException("assemblyPath");
            }
            this.AssemblyPath = assemblyPath;
            if (string.IsNullOrEmpty(workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(this.AssemblyPath);
            }
            this.WorkingDirectory = workingDirectory;
            this.Arguments = arguments;
        }

        /// </inheritdoc>
        internal override void GetReadyToEnqueue() {
            string[] scargs;
            string[] appargs;

            AppProcess.ParseArguments(this.Arguments, out scargs, out appargs);
            this.ArgumentsToStarcounter = scargs;
            this.ArgumentsToApplication = appargs;
            
            if (string.IsNullOrEmpty(this.DatabaseName)) {
                string databaseArg;
                databaseArg = scargs.FirstOrDefault<string>(delegate(string candidate) {
                    return candidate != null && candidate.StartsWith("Db=");
                });
                if (databaseArg != null) {
                    databaseArg = databaseArg.Substring(databaseArg.IndexOf("=") + 1);
                    this.DatabaseName = databaseArg;
                } else {
                    this.DatabaseName = Path.GetFileNameWithoutExtension(this.AssemblyPath);
                }
            }

            if (this.NoDb == false) {
                this.NoDb = scargs.Contains<string>("NoDb", StringComparer.InvariantCultureIgnoreCase);
            }
        }
    }
}