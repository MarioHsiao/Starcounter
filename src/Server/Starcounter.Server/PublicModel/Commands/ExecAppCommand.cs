// ***********************************************************************
// <copyright file="ExecAppCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using System.Linq;
using Starcounter.Internal;
using Starcounter.Apps.Bootstrap;

namespace Starcounter.Server.PublicModel.Commands {

    /// <summary>
    /// A command representing the request to start an executable.
    /// </summary>
    public sealed class ExecAppCommand : DatabaseCommand {
        string databaseName;

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
        /// Gets or sets a value instructing the processor of this command to
        /// prepare the hosting of the specified assembly, but never issue the
        /// call to actually host it.
        /// </summary>
        /// <remarks>
        /// Used by the infrastructure in the development integration to be
        /// able to run everything up until hosting, and then attach the debugger
        /// before actually hosting the assembly.
        /// </remarks>
        public bool PrepareOnly {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the server should apply
        /// the "LogSteps" switch to the code host process in which the
        /// executable represented by this command is to be hosted.
        /// </summary>
        public bool LogSteps {
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
            : base(engine, null, "Starting {0}", Path.GetFileName(assemblyPath)) {
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

        /// <inheritdoc />
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
                    this.DatabaseName = StarcounterConstants.DefaultDatabaseName;
                }
            }

            if (this.NoDb == false) {
                this.NoDb = scargs.Contains<string>("NoDb", StringComparer.InvariantCultureIgnoreCase);
            }

            if (this.LogSteps == false) {
                this.LogSteps = scargs.Contains<string>("LogSteps", StringComparer.InvariantCultureIgnoreCase);
            }

            base.GetReadyToEnqueue();
        }
    }
}