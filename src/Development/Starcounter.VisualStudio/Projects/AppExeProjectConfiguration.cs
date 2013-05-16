
using EnvDTE;
using EnvDTE90;
using Microsoft.VisualStudio.Shell.Interop;
using Starcounter.Internal;
using Starcounter.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.Projects {
    using Starcounter.Advanced;
    using Starcounter.CommandLine;
    using Starcounter.CommandLine.Syntax;
    using Starcounter.Rest.ExtensionMethods;
    using Starcounter.Server.Rest;
    using Starcounter.Server.Rest.Representations.JSON;
    using System.Net.Sockets;
    using EngineReference = Starcounter.Server.Rest.Representations.JSON.EngineCollection.EnginesApp;
    using ExecutableReference = Starcounter.Server.Rest.Representations.JSON.Engine.ExecutablesApp.ExecutingApp;
    using Option = Starcounter.Server.SharedCLI.Option;

    /// <summary>
    /// Provides a set of methods that governs the handling category
    /// of errors occuring in the HTTP traffic when running the debug
    /// sequence.
    /// </summary>
    static class HTTPHelp {
        public const string CRLF = "\r\n";
    }

    [ComVisible(false)]
    internal class AppExeProjectConfiguration : StarcounterProjectConfiguration {
        static IApplicationSyntax commandLineSyntax;
        readonly IVsOutputWindowPane outputWindowPane = null;
        bool debugFlagSpecified = false;

        internal static void Initialize() {
            RequestHandler.InitREST();

            var appSyntax = new ApplicationSyntaxDefinition();
            appSyntax.DefaultCommand = "exec";
            SharedCLI.DefineWellKnownOptions(appSyntax, true);

            // Hidden "exec" command, allowing us to use the command-line
            // library for the parsing and defining of the command-line given
            // in project settings
            appSyntax.DefineCommand("exec", "Executes the application", 0, int.MaxValue);
            AppExeProjectConfiguration.commandLineSyntax = appSyntax.CreateSyntax();
        }

        void HandleUnexpectedResponse(Response response) {
            ErrorMessage msg;
            try {
                var detail = new ErrorDetail();
                detail.PopulateFromJson(response.GetBodyStringUtf8_Slow());
                msg = ErrorMessage.Parse(detail.Text);

            } catch {
                // With any kind of failure interpreting the response
                // message, we use the general error code and include the
                // full response as the postfix in the message.
                msg = ErrorCode.ToMessage(Error.SCERRDEBUGFAILEDREPORTED, response.ToString());
            }

            this.ReportError((ErrorMessage)msg);
            throw ErrorCode.ToException(Error.SCERRDEBUGFAILEDREPORTED);
        }

        public AppExeProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration)
            : base(package, project, baseConfiguration, innerConfiguration) {
                this.outputWindowPane = package.StarcounterOutputPane;
        }

        protected override bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        protected override bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            DateTime start = DateTime.Now;
            ApplicationArguments cmdLine;
            string serverHost;
            int serverPort;
            string serverName;
            bool result;

            var debugConfiguration = new AssemblyDebugConfiguration(this);
            if (!debugConfiguration.IsStartProject) {
                throw new NotSupportedException("Only 'Project' start action is currently supported.");
            }

            var parser = new Parser(debugConfiguration.Arguments);
            cmdLine = parser.Parse(commandLineSyntax);
            if (cmdLine.ContainsFlag(SharedCLI.UnofficialOptions.Debug)) {
                debugFlagSpecified = true;
                System.Diagnostics.Debugger.Launch();
            }

            // Assure personal server is running; will be done as soon
            // as Christian has implemented the code to do so.
            // TODO:

            // Pass it on:
            try {
                result = DoBeginDebug(debugConfiguration, flags, cmdLine);
            } catch (SocketException se) {
                // Map the socket level error code to a correspoding Starcounter
                // error code. Try to be as specific as possible.
                uint scErrorCode;
                switch (se.SocketErrorCode) {
                    case SocketError.ConnectionRefused:
                        scErrorCode = Error.SCERRSERVERNOTRUNNING;
                        break;
                    default:
                        scErrorCode = Error.SCERRSERVERNOTAVAILABLE;
                        break;
                }

                SharedCLI.ResolveAdminServer(cmdLine, out serverHost, out serverPort, out serverName);
                var serverInfo = string.Format("\"{0}\" at {1}:{2}", serverName, serverHost, serverPort);

                this.ReportError((ErrorMessage)ErrorCode.ToMessage(scErrorCode, string.Format("(Server: {0})", serverInfo)));
                result = false;
            }

            if (result) {
                var finish = DateTime.Now;
                this.WriteLine("Successfully started {0} (time {1}s), using parameters {2}", 
                    Path.GetFileName(debugConfiguration.AssemblyPath),
                    finish.Subtract(start).ToString(@"ss\.fff"),
                    string.Join(" ", debugConfiguration.Arguments));
            }
            return result;
        }

        bool DoBeginDebug(AssemblyDebugConfiguration debugConfig, __VSDBGLAUNCHFLAGS flags, ApplicationArguments args) {
            string serverHost;
            int serverPort;
            string serverName;
            string databaseName;
            EngineReference engineRef;
            Engine engine;
            ErrorDetail errorDetail;
            int statusCode;
            string headers;

            ResponseExtensions.OnUnexpectedResponse = this.HandleUnexpectedResponse;
            SharedCLI.ResolveAdminServer(args, out serverHost, out serverPort, out serverName);
            SharedCLI.ResolveDatabase(args, out databaseName);
            var admin = new AdminAPI();
            var uris = admin.Uris;
            var node = new Node(serverHost, (ushort)serverPort);

            // Get the running engine on the server. If it's not found, we take
            // a step back, and either create the database and/or start the engine.
           
            this.debugLaunchDescription = string.Format(
                "Starting \"{0}\" in database {1}:{2}/{3}.",
                Path.GetFileName(debugConfig.AssemblyPath),
                serverHost, serverPort, databaseName);
            this.WriteDebugLaunchStatus("Verifying database engine");

            // GET or START the engine
            var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
            statusCode = response.FailIfNotSuccessOr(404);
            if (statusCode == 404) {
                errorDetail = new ErrorDetail();
                errorDetail.PopulateFromJson(response.GetBodyStringUtf8_Slow());
                if (errorDetail.ServerCode == Error.SCERRDATABASENOTFOUND) {
                    var allowed = !args.ContainsFlag(Option.NoAutoCreateDb);
                    if (!allowed) {
                        throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND,
                            string.Format("Database: {0}. Remove {1} to create automatically.", databaseName, Option.NoAutoCreateDb));
                    }
                    WriteDebugLaunchStatus("Creating database");
                    CreateDatabase(node, uris, databaseName);
                }

                this.WriteDebugLaunchStatus("Starting engine");
                engineRef = new EngineReference();
                engineRef.Name = databaseName;
                engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);
                response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                response.FailIfNotSuccess();
            }
            engine = new Engine();
            engine.PopulateFromJson(response.GetBodyStringUtf8_Slow());
            var engineETag = response["ETag"];

            // The engine is now started. Check if the executable we
            // are about to debug is part of it. If so, we must restart
            // it. The restart is conditional, to support multiple
            // exeuctable scenarios. See below.

            ExecutableReference exeRef = engine.GetExecutable(debugConfig.AssemblyPath);
            if (exeRef != null) {
                var restart = true;
                headers = string.Format("ETag: {0}{1}", engineETag, HTTPHelp.CRLF);
                this.WriteDebugLaunchStatus("Stopping engine");
                response = node.DELETE(node.ToLocal(engine.CodeHostProcess.Uri), null, headers, null);
                response.FailIfNotSuccessOr(404, 412);
                if (response.StatusCode == 412) {
                    // Precondition failed. We expect someone else to have stopped
                    // or restared the engine, and that our executable is no longer
                    // part of it. If it still is, we have no ability to figure out
                    // what just happened and we must fail the attempt to debug.
                    response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
                    response.FailIfNotSuccessOr(404);
                    if (response.IsSuccessStatusCode) {
                        engine.PopulateFromJson(response.GetBodyStringUtf8_Slow());
                        var exeHasStopped = engine.GetExecutable(debugConfig.AssemblyPath);
                        if (exeHasStopped != null) {
                            // This we just can't handle.
                            // One alternative might be to try this a few times, to see
                            // if we get a better result the second or third, but lets
                            // just don't do that right now.
                            // TODO: Craft a proper error message.
                            throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, "Engine was restarted/modified, and exe still in it. Aborting.");
                        }

                        restart = false;
                    }
                }
                if (restart) {
                    this.WriteDebugLaunchStatus("Starting engine");
                    engineRef = new EngineReference();
                    engineRef.Name = databaseName;
                    engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                    engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                    response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                    response.FailIfNotSuccess();
                    engine.PopulateFromJson(response.GetBodyStringUtf8_Slow());
                }
            }

            if ((flags & __VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) == 0) {
                this.WriteDebugLaunchStatus("Attaching debugger");
                if (!AttachDebugger(engine)) {
                    return false;
                }
            }

            this.WriteDebugLaunchStatus("Starting executable");
            var exe = new Executable();
            exe.IsTool = false;
            exe.Path = debugConfig.AssemblyPath;
            exe.StartedBy = "Per Samuelsson (per@starcounter.com)";
            foreach (var arg in args.CommandParameters.ToArray()) {
                exe.Arguments.Add().dummy = arg;
            }
            
            // To run the whole starting of the executable asynchrnously,
            // enable the following header:
            // headers = string.Format("Expect: {0}{1}", "202-accepted", HTTPHelp.CRLF);
            headers = null;
            response = node.POST(node.ToLocal(engine.Executables.Uri), exe.ToJson(), headers, null);
            response.FailIfNotSuccess();

            return true;
        }

        bool AttachDebugger(Engine engine) {
            DTE dte;
            bool attached;
            string errorMessage;
            
            try {
                dte = this.package.DTE;
                var debugger = (Debugger3)dte.Debugger;
                attached = false;

                foreach (Process3 process in debugger.LocalProcesses) {
                    if (process.ProcessID == engine.CodeHostProcess.PID) {
                        process.Attach();
                        attached = true;
                        break;
                    }
                }

                if (attached == false) {
                    this.ReportError(
                        "Cannot attach the debugger to the database {0}. Process {1} not found.",
                        engine.Database.Name,
                        engine.CodeHostProcess.PID
                        );
                }

                return attached;

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
                        engine.Database.Name, engine.CodeHostProcess.PID);
                    errorMessage +=
                        "The database runs with higher privileges than Visual Studio. Either restart Visual Studio " +
                        "and run it as an administrator, or make sure the database runs in non-elevated mode.";

                    this.ReportError(errorMessage);
                    return false;

                } else if (comException.ErrorCode == -2147221503) {
                    // The COM exception raised when trying to attach to a
                    // process with a debugger already attached to it.
                    this.ReportError((ErrorMessage)ErrorCode.ToMessage(Error.SCERRDEBUGGERALREADYATTACHED));
                    return false;
                }

                throw comException;
            }
        }

        static void CreateDatabase(Node node, AdminAPI.ResourceUris uris, string databaseName) {
            var db = new Database();
            db.Name = databaseName;
            var response = node.POST(uris.Databases, db.ToJson(), null, null);
            response.FailIfNotSuccess();
        }

        protected override void WriteDebugLaunchStatus(string status) {
            base.WriteDebugLaunchStatus(status);
            if (debugFlagSpecified) {
                WriteLine(status);
            }
        }

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
            info.bstrExe = Path.Combine(BaseVsPackage.InstallationDirectory, "sccode.exe");
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