
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using Starcounter.Internal;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Server.PublicModel;
using System.Threading;
using EnvDTE;
using EnvDTE90;

namespace Starcounter.VisualStudio.Projects {
    using Thread = System.Threading.Thread;

    [ComVisible(false)]
    internal class AppExeProjectConfiguration : StarcounterProjectConfiguration {
        readonly IVsOutputWindowPane outputWindowPane = null;

        public AppExeProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration)
            : base(package, project, baseConfiguration, innerConfiguration) {
                this.outputWindowPane = package.StarcounterOutputPane;
        }

        protected override bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        protected override bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            CommandInfo command = null;
            DateTime start = DateTime.Now;
            
            var debugConfiguration = new AssemblyDebugConfiguration(this);
            if (!debugConfiguration.IsStartProject) {
                throw new NotSupportedException("Only 'Project' start action is currently supported.");
            }

            // Create a client object to be able to communicate with the server.
            // We use the built-in ABCIPC core services of the server to execute
            // assemblies.

            var pipeName = ScUriExtensions.MakeLocalServerPipeString("personal");
            var client = ClientServerFactory.CreateClientUsingNamedPipes(pipeName);

            properties.Add("AssemblyPath", debugConfiguration.AssemblyPath);
            properties.Add("WorkingDir", debugConfiguration.WorkingDirectory);
            if (debugConfiguration.Arguments.Length > 0) {
                properties.Add("Args", KeyValueBinary.FromArray(debugConfiguration.Arguments).Value);
            }
            
            // Send the request to the server and dezerialize the reply
            // to get the information about the command.
            //   Then iterate until we see that the command has completed,
            // and evaluate the result.
            //
            // We must get a way to check if anything lengthy will be
            // needed when doing this, like creating a database, weaving,
            // copying, or if the database is big and the schema must
            // change. This is b/c if so, we must do this in another thread,
            // or else the GUI will be locked up and be non-responsive.

            this.debugLaunchDescription = string.Format("Requesting \"{0}\" to be hosted in Starcounter", Path.GetFileName(debugConfiguration.AssemblyPath));
            this.WriteDebugLaunchStatus(null);

            client.Send("ExecApp", properties, (Reply reply) => {
                if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                command = ServerUtility.DeserializeCarry<CommandInfo>(reply);
            });

            int threadSuspensionTimeout = 650;
            int triesBeforeSwitchingThread = int.MaxValue;
#if false
            threadSuspensionTimeout = 200;
            triesBeforeSwitchingThread = 10;
#endif
            for (int i = 0; i < triesBeforeSwitchingThread; i++) {
                if (command.IsCompleted)
                    break;

                Thread.Sleep(threadSuspensionTimeout);

                client.Send("GetCompletedCommand", command.Id.ToString(), (Reply reply) => {
                    if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                    if (reply.HasCarry) {
                        command = ServerUtility.DeserializeCarry<CommandInfo>(reply);
                    }
                });
            }

#if false
            if (!command.IsCompleted) {
                // We are switching to another thread. We must pass the
                // command identity and the debug information along, since we'll
                // need it when we must get back to the GUI thread again.
                //
                // If we decide to use this option, we should probably
                // consider keeping a dedicated thread around, since it can
                // indicate a long running task, and we shouldn't lock one
                // from the pool up.

                ThreadPool.QueueUserWorkItem(this.WaitForExecAssemblyCommandAndThenCallLauchDebugger, new object[] { client, command.Id, flags, debugConfiguration });
                return true;
            }
#endif
            // Invoke the method actually attaching the debugger, after
            // we have assured the server has fully processed the exec assembly
            // command.
            
            LaunchDebugEngineIfExecCommandSucceeded(client, command, flags, debugConfiguration);
            
            var finish = DateTime.Now;
            this.WriteLine("Debug sequence time: {0}, using parameters {1}", finish.Subtract(start), string.Join(" ", debugConfiguration.Arguments));
            
            return true;
        }

        void LaunchDebugEngineIfExecCommandSucceeded(
            Client client,
            CommandInfo execResult,
            __VSDBGLAUNCHFLAGS flags, 
            AssemblyDebugConfiguration debugConfiguration) {
            DTE dte;
            bool attached;
            string errorMessage;
            DatabaseInfo database;

            // We now have the final result of the command.
            // If it succeeded (i.e. has no errors) we should be able to get the
            // database from the command.
            if (execResult.HasError)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, execResult.Errors[0].ToString());

            // The exec command succeeded. It should mean we could now get
            // the database information, including all we need to attach the
            // debugger to the host process

            this.debugLaunchDescription = string.Format("Retreiving info of database \"{0}\"", execResult.DatabaseUri);
            this.WriteDebugLaunchStatus(null);

            database = null;
            client.Send("GetDatabase", ScUri.FromString(execResult.DatabaseUri).DatabaseName, (Reply reply) => {
                if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                database = ServerUtility.DeserializeCarry<DatabaseInfo>(reply);
            });

            this.debugLaunchDescription = string.Format("Attaching the debugger to database \"{0}\"", database.Name);
            this.WriteDebugLaunchStatus(null);

            try {
                dte = this.package.DTE;
                var debugger = (Debugger3)dte.Debugger;
                attached = false;

                foreach (Process3 process in debugger.LocalProcesses) {
                    if (process.ProcessID == database.HostProcessId) {
                        process.Attach();
                        attached = true;
                        break;
                    }
                }

                if (attached == false) {
                    this.ReportError(
                        "Cannot attach the debugger to the database {0}. Process {1} not found.",
                        database.Name,
                        database.HostProcessId
                        );
                    return;
                }
            } catch (COMException comException) {
                if (comException.ErrorCode == -2147221447) {
                    // "Exception from HRESULT: 0x80040039"
                    //
                    // Occurs when the database runs with higher privileges than the user
                    // running Visual Studio. In that case, the debugger is not allowed to
                    // attach.
                    //
                    // http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.externalexception.errorcode(v=VS.90).aspx
                    // http://blogs.msdn.com/b/joshpoley/archive/2008/01/04/errors-004-facility-itf.aspx
                    // http://msdn.microsoft.com/en-us/library/ms734241(v=vs.85).aspx

                    errorMessage = string.Format(
                        "Attaching the debugger to the database \"{0}\" in process {1} was not allowed. ",
                        database.Name, database.HostProcessId);
                    errorMessage +=
                        "The database runs with higher privileges than Visual Studio. Either restart Visual Studio " +
                        "and run it as an administrator, or make sure the database runs in non-elevated mode.";

                    this.ReportError(errorMessage);
                    return;
                }

                throw comException;
            } finally {
                this.debugLaunchPending = false;
            }

            // The database is in fact running and the debugger is attached.
            // We are done.

            this.debugLaunchDescription = null;
            WriteDebugLaunchStatus(null);
        }

#if false
        /// <summary>
        /// Continues waiting for the ExecApp command, that seems to be taking
        /// some time (possibly creating a new database, weaving, etc).
        /// </summary>
        /// <remarks>
        /// This method is called from a background thread, during the debug
        /// launching sequence, joining back with the main GUI thread once the
        /// server has finished processing the request.
        /// </remarks>
        /// <param name="state">The state we operate on (array of values).</param>
        void WaitForExecAssemblyCommandAndThenCallLauchDebugger(object state) {
            object[] args = (object[])state;
            var client = (Client)args[0];
            var commandId = (CommandId)args[1];
            var flags = (__VSDBGLAUNCHFLAGS)args[2];
            var debugConfiguration = (AssemblyDebugConfiguration)args[3];
            CommandInfo command = null;

            do {
                Thread.Sleep(500);
                client.Send("GetCompletedCommand", commandId.ToString(), (Reply reply) => {
                    if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                    if (reply.HasCarry) {
                        command = ServerUtility.DeserializeCarry<CommandInfo>(reply);
                    }
                });
            } while (!command.IsCompleted);

            this.package.BeginInvoke(() => {
                LaunchDebugEngineIfExecCommandSucceeded(client, command, flags, debugConfiguration);
            });
        }
#endif

        /// <summary>
        /// Writes a message to the default underlying output pane (normally
        /// the Starcounter output pane).
        /// </summary>
        /// <param name="format">The message to write.</param>
        /// <param name="args">Message arguments</param>
        public void WriteLine(string format, params object[] args) {
            this.WriteLine(string.Format(format, args));
        }

        /// <summary>
        /// Writes a message to the default underlying output pane (normally
        /// the Starcounter output pane).
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void WriteLine(string message) {
            if (this.outputWindowPane == null) {
                return;
            }
            this.outputWindowPane.OutputStringThreadSafe(message + Environment.NewLine);
        }

#if false
        // This is the preferred way to attach the debugger to our database,
        // but I just can't get it to work b/c of, I think, the database is a
        // 64-bit process. I get the exact same symptoms as in this stackoverflow
        // post:
        // http://stackoverflow.com/questions/9523251/visual-studio-custom-debug-engine-attach-to-a-64-bit-process
        // And no one seems to address it and/or respond.
        // 
        // Meanwhile, we can use the above attachment method (using process.Attach)
        // but we lack in functionality, such as finetuning the attachment options,
        // so I'll try to get back to this and experiement some more + find if we
        // should go up one more version (Debugger3+LaunchDebugTargets3) instead, but
        // right now there is no time. :(

        void LaunchDebugEngine2(__VSDBGLAUNCHFLAGS flags, AssemblyDebugConfiguration debugConfiguration, DatabaseInfo database) {
            var debugger = (IVsDebugger2)package.GetService(typeof(SVsShellDebugger));
            var info = new VsDebugTargetInfo2();

            info.cbSize = (uint)Marshal.SizeOf(info);
            info.bstrExe = Path.Combine(BaseVsPackage.InstallationDirectory, "boot.exe");
            info.dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_AlreadyRunning;
            info.LaunchFlags = (uint) flags;
            info.dwProcessId = (uint)database.HostProcessId;
            info.bstrRemoteMachine = null;
            info.fSendToOutputWindow = 0;

            IntPtr pInfo = Marshal.AllocCoTaskMem((int)info.cbSize);
            Marshal.StructureToPtr(info, pInfo, false);

            try {
                int errorcode = debugger.LaunchDebugTargets2(1, pInfo);
                if (errorcode != VSConstants.S_OK) {
                    string errorInfo = this.package.GetErrorInfo();
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Failed to attach debug engine ({0}={1})", errorcode, errorInfo));
                }
            } finally {
                if (pInfo != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pInfo);
                }
            }
        }
#endif
    }
}