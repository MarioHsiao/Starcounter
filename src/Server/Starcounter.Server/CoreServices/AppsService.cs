// ***********************************************************************
// <copyright file="AppsService.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Apps.Bootstrap;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates the functionality of the server Apps service,
    /// running as part of the engine, accepting local Apps executables
    /// to dispatch execution requests from the shell.
    /// </summary>
    internal sealed class AppsService {
        readonly ServerEngine engine;

        /// <summary>
        /// Intializes a new <see cref="AppsService"/>.
        /// </summary>
        /// <param name="engine">The engine in which the current service
        /// runs.</param>
        internal AppsService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the <see cref="AppsService"/>.
        /// </summary>
        internal void Setup() {
        }

        /// <summary>
        /// Stops the current <see cref="AppsService"/>, meaning the engine
        /// in which it runs no longer will accept Apps requests.
        /// </summary>
        internal void Stop() {
            // Implement stopping of the request thread.
            // TODO:
        }

        /// <summary>
        /// Enques a request to start an exacable with the dispatcher of the
        /// <see cref="ServerEngine"/> in which this service runs.
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal CommandInfo EnqueueExecAppCommandWithDispatcher(string assemblyPath, string workingDirectory, string[] args) {
            ExecCommand command = new ExecCommand(this.engine, assemblyPath, workingDirectory, args);
            return engine.CurrentPublicModel.Execute(command);
        }
    }
}