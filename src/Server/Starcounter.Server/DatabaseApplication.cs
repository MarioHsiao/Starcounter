﻿
using Starcounter.Bootstrap.Management.Representations.JSON;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Represents an application as it relates to a <see cref="Database"/>
    /// under a given server.
    /// </summary>
    internal sealed class DatabaseApplication {
        /// <summary>
        /// Holds the application information for the current database
        /// application, as kept by the server engine.
        /// </summary>
        public readonly AppInfo Info;

        /// <summary>
        /// Initialize a new <see cref="DatabaseApplication"/> based
        /// on the given application information.
        /// </summary>
        /// <param name="info"></param>
        public DatabaseApplication(AppInfo info) {
            if (info == null) {
                throw new ArgumentNullException("info");
            }
            Info = info;
        }

        /// <summary>
        /// Gets or sets a value indicating if the current application was,
        /// or will be, started with its entrypoint being invoked asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>The default is <c>false</c>. Normally, the entrypoint of any
        /// application is run in a synchronous fashion.</para>
        /// <para>If the application doesn't define an entrypoint - for example,
        /// it's represented by a library or just some code file with a few
        /// classes - this property is silently ignored.</para>
        /// </remarks>
        internal bool IsStartedWithAsyncEntrypoint {
            get;
            set;
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="DatabaseApplication"/> in the
        /// form of a public model <see cref="AppInfo"/>.
        /// </summary>
        /// <returns>An <see cref="AppInfo"/> representing the current state
        /// of this executable.</returns>
        internal AppInfo ToPublicModel() {
            return Info.DeepClone();
        }

        /// <summary>
        /// Creates an <see cref="Executable"/> instance based on the
        /// properties of the current <see cref="DatabaseApplication"/>.
        /// </summary>
        /// <returns>An <see cref="Executable"/> representing the same
        /// application as the current instance.</returns>
        internal Executable ToExecutable() {
            var info = Info;
            var exe = new Executable();
            exe.Path = this.Info.ExecutionPath;

            exe.PrimaryFile = this.Info.BinaryFilePath;
            exe.ApplicationFilePath = this.Info.ApplicationFilePath;
            exe.Name = this.Info.Name;
            exe.WorkingDirectory = this.Info.WorkingDirectory;
            if (this.Info.Arguments != null) {
                foreach (var argument in this.Info.Arguments) {
                    exe.Arguments.Add().dummy = argument;
                }
            }
            exe.RunEntrypointAsynchronous = this.IsStartedWithAsyncEntrypoint;
            return exe;
        }
    }
}
