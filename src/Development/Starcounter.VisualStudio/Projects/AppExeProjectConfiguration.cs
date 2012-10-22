
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

        public AppExeProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration)
            : base(package, project, baseConfiguration, innerConfiguration) {
        }

        protected override bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        protected override bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            CommandInfo command = null;
            DatabaseInfo database = null;

            var debugConfiguration = new AssemblyDebugConfiguration(this);
            if (!debugConfiguration.IsStartProject) {
                throw new NotSupportedException("Only 'Project' start action is currently supported.");
            }

            // Create a client object to be able to communicate with the server.
            // We use the built-in ABCIPC core services of the server to execute
            // assemblies.
            
            var client = ClientServerFactory.CreateClientUsingNamedPipes(
                string.Format("sc//{0}/{1}", Environment.MachineName, "personal").ToLowerInvariant());

            properties.Add("AssemblyPath", debugConfiguration.AssemblyPath);
            properties.Add("WorkingDir", debugConfiguration.WorkingDirectory);
            if (debugConfiguration.Arguments.Length > 0) {
                properties.Add("Args", KeyValueBinary.FromArray(debugConfiguration.Arguments).Value);
            }
            
            // Send the request to the server and dezerialize the reply
            // to get the information about the command.
            //   Then iterate until we see that the command has completed,
            // and evaluate the result.
            
            client.Send("ExecApp", properties, (Reply reply) => {
                if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                command = ServerUtility.DeserializeCarry<CommandInfo>(reply);
            });

            while (!command.IsCompleted) {
                Thread.Sleep(650);

                client.Send("GetCompletedCommand", command.Id.ToString(), (Reply reply) => {
                    if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                    if (reply.HasCarry) {
                        command = ServerUtility.DeserializeCarry<CommandInfo>(reply);
                    }
                });
            }

            // We now have the final result of the command.
            // If it succeeded (i.e. has no errors) we should be able to get the
            // database from the command.
            if (command.HasError)
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, command.Errors[0].ToString());

            // The exec command succeeded. It should mean we could now get
            // the database information, including all we need to attach the
            // debugger to the host process

            client.Send("GetDatabase", ScUri.FromString(command.DatabaseUri).DatabaseName, (Reply reply) => {
                if (!reply.IsSuccess) throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, reply.ToString());
                database = ServerUtility.DeserializeCarry<DatabaseInfo>(reply);
            });
            
            LaunchDebugEngine(flags, debugConfiguration, database);
            return true;
        }

        void LaunchDebugEngine(__VSDBGLAUNCHFLAGS flags, AssemblyDebugConfiguration debugConfiguration, DatabaseInfo database) {
            DTE dte;
            bool attached;
            string errorMessage;

            this.debugLaunchDescription = string.Format("Attaching the debugger to database {0}", database.Name);
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
            // Start the configured startup program.

            this.debugLaunchDescription = "Starting configured startup program";
            WriteDebugLaunchStatus(null);
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