
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
        /// Starts the current <see cref="AppsService"/>, meaning the
        /// engine in which it runs will accept requests being sent.
        /// </summary>
        /// <remarks>
        /// This method does not block, but will instead return control
        /// to the host right after the service has started.
        /// </remarks>
        internal void Start() {
            AppProcess.WaitForStartRequests(OnIncomingAppExeStartRequest);
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
            ExecAppCommand command = new ExecAppCommand(this.engine, assemblyPath, workingDirectory, args);
            return engine.CurrentPublicModel.Execute(command);
        }

        /// <summary>
        /// Handles requests coming from booting App executables.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        bool OnIncomingAppExeStartRequest(Dictionary<string, string> properties) {
            string assemblyPath;
            string workingDirectory;
            string serializedArgs;
            string[] arguments;

            // Validate all required properties are given in the given
            // property set and only enque a command if they are.
            //
            // If any of them are missing, we simply log a warning and keep
            // listening for new requests.

            if (!properties.TryGetValue("AssemblyPath", out assemblyPath)) {
                //LogWarning("Ignoring starting request without given assembly path.");
                return true;
            }

            if (!properties.TryGetValue("WorkingDirectory", out workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(assemblyPath);
            }

            if (properties.TryGetValue("Args", out serializedArgs)) {
                arguments = KeyValueBinary.ToArray(serializedArgs);
            } else {
                arguments = new string[0];
            }

            EnqueueExecAppCommandWithDispatcher(assemblyPath, workingDirectory, arguments);
            return true;
        }
    }
}