// ***********************************************************************
// <copyright file="DatabaseApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Represents an "App" as it relates to a <see cref="Database"/> under
    /// a given server.
    /// </summary>
    internal sealed class DatabaseApp {
        /// <summary>
        /// Gets or sets the path to the original executable, i.e. the
        /// executable that caused the materialization of this instance.
        /// </summary>
        internal string OriginalExecutablePath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the server key for this executable. A key must
        /// be assured to be unique within the scope of a single database.
        /// </summary>
        internal string Key { 
            get; 
            set;
        }

        /// <summary>
        /// Gets or sets the path to the executable file actually being
        /// loaded into the database, i.e. normally to the file after it
        /// has been weaved.
        /// </summary>
        internal string ExecutionPath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the working directory of the App.
        /// </summary>
        internal string WorkingDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full argument set passed to the executable when
        /// started, possibly including both arguments targeting Starcounter
        /// and/or the actual App Main.
        /// </summary>
        internal string[] Arguments {
            get;
            set;
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="DatabaseApp"/> in the
        /// form of a public model <see cref="AppInfo"/>.
        /// </summary>
        /// <returns>An <see cref="AppInfo"/> representing the current state
        /// of this executable.</returns>
        internal AppInfo ToPublicModel() {
            return new AppInfo() {
                ExecutablePath = this.OriginalExecutablePath,
                ExecutionPath = this.ExecutionPath,
                Key = this.Key
            };
        }
    }
}
