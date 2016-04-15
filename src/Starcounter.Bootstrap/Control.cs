// ***********************************************************************
// <copyright file="Control.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Starcounter.CommandLine;
using Starcounter; // TODO:
using Starcounter.Advanced;
using Starcounter.Hosting;
using Starcounter.Internal; // TODO:
using Starcounter.Logging;
using StarcounterInternal.Hosting;
using System.Text.RegularExpressions;
using System.IO;
using Starcounter.Bootstrap.Management;
using Starcounter.Ioc;

namespace StarcounterInternal.Bootstrap {
    /// <summary>
    /// Class Control
    /// </summary>
    public class Control // TODO: Make internal.
    {
        /// <summary>
        /// Log source to be used when logging/tracing application
        /// level events.
        /// </summary>
        public static LogSource ApplicationLogSource;

        /// <summary>
        /// The log source established at the time of creation of the
        /// Control instance;
        /// </summary>
        readonly LogSource log;

        private Control(LogSource logSource) {
            log = logSource;
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args) {
            try {
                //Debugger.Launch();

                Control c = new Control(Control.ApplicationLogSource);
                c.OnProcessInitialized();
                bool b = c.Setup(args);
                if (b) {
                    c.Start();
                    c.Run();
                    c.Stop();
                    c.Cleanup();
                }
            }
            catch (Exception ex) {
                if (!StarcounterInternal.Hosting.ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        /// <summary>
        /// Loaded configuration info.
        /// </summary>
        private Configuration configuration;

        /// <summary>
        /// The withdb_
        /// </summary>
        private bool withdb_;

        /// <summary>
        /// The hsched_
        /// </summary>
        private unsafe void* hsched_;

        unsafe delegate UInt32 HandleManagedDelegate(
            UInt64 handlerInfo,
            Byte* rawChunk,
            bmx.BMX_TASK_INFO* taskInfo,
            Boolean* isHandled);

        delegate void DestroyAppsSessionCallback(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt
            );

        delegate void CreateNewAppsSessionCallback(ref ScSessionStruct ss);

        unsafe delegate void ErrorHandlingCallback(
            UInt32 err_code,
            Char* err_string,
            Int32 err_string_len
            );

        /// <summary>
        /// Setups the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private unsafe bool Setup(string[] args) {
            try {

#if false
            // Disables priority boost for all the threads in the process.
            // Often a good idea when using spin-locks. Not sure it worth
            // anything with the current setup however since most often no more
            // running threads then cores. So leaving this disabled for now.

            Kernel32.SetProcessPriorityBoost(Kernel32.GetCurrentProcess(), 1);
#endif
                StarcounterInternal.Hosting.ExceptionManager.Init();

                DatabaseExceptionFactory.InstallInCurrentAppDomain();
                OnExceptionFactoryInstalled();

                ApplicationArguments arguments;
                ProgramCommandLine.TryGetProgramArguments(args, out arguments);
                OnCommandLineParsed();

                if (arguments.ContainsFlag("attachdebugger")) {
                    Debugger.Launch();
                }

                configuration = Configuration.Load(arguments);
                OnConfigurationLoaded();

                withdb_ = !configuration.NoDb;
                StarcounterEnvironment.Gateway.NumberOfWorkers = configuration.GatewayNumberOfWorkers;
                StarcounterEnvironment.NoNetworkGatewayFlag = configuration.NoNetworkGateway;

                AssureNoOtherProcessWithTheSameName(configuration);
                OnAssuredNoOtherProcessWithTheSameName();

                uint schedulerCount = configuration.SchedulerCount;
                uint memSize = CalculateAmountOfMemoryNeededForRuntimeEnvironment(schedulerCount);
                byte* mem = (byte*)Kernel32.VirtualAlloc((void*)0, (IntPtr)memSize, Kernel32.MEM_COMMIT, Kernel32.PAGE_READWRITE);
                OnGlobalMemoryAllocated();

                ulong hlogs = ConfigureLogging(configuration);
                var activateTraceLogging = Diagnostics.IsGlobalTraceLoggingEnabled 
                    || arguments.ContainsFlag(StarcounterConstants.BootstrapOptionNames.EnableTraceLogging);
                if (activateTraceLogging) {
                    System.Diagnostics.Trace.Listeners.Add(new LogTraceListener());
                }
                OnLoggingConfigured();

                ManagementService.Init(configuration.Name);

                // Initializing the BMX manager if network gateway is used.
                if (!configuration.NoNetworkGateway) {

                    HandleManagedDelegate managed_delegate = GatewayHandlers.HandleManaged;
                    GCHandle globally_allocated_handler = GCHandle.Alloc(managed_delegate);
                    IntPtr pinned_delegate = Marshal.GetFunctionPointerForDelegate(managed_delegate);

                    bmx.sc_init_bmx_manager(pinned_delegate);

                    OnBmxManagerInitialized();

                    // Initializing package loader.
                    Package.InitPackage(() => {

                        SqlRestHandler.Register(
                            configuration.DefaultUserHttpPort,
                            configuration.DefaultSystemHttpPort);

                        // Register console output handlers (Except for the Administrator)
                        if (!StarcounterEnvironment.IsAdministratorApp) {
                            ConsoleOuputRestHandler.Register(configuration.DefaultUserHttpPort, configuration.DefaultSystemHttpPort);
                            Profiler.SetupHandler(configuration.DefaultSystemHttpPort, Db.Environment.DatabaseNameLower);
                        }
                    });
                }

                // Configuring host environment.
                ConfigureHost(configuration, hlogs);
                OnHostConfigured();

                // Configuring schedulers.
                hsched_ = ConfigureScheduler(configuration, mem, schedulerCount);
                mem += (1024 + (schedulerCount * 512));
                OnSchedulerConfigured();

                // Initialize the Db environment (database name)
                Db.SetEnvironment(new DbEnvironment(configuration.Name, withdb_));

                // Create and initialize the default host for the current
                // process. This enables services to be installed/retreived,
                // so possibly we should do this earlier once we switch to
                // a more DI-based envioronment.
                DefaultHost.InstallCurrent();

                // Initializing system profilers.
                Profiler.Init();

                // Applying configuration flags for applications.
                configuration.ApplyAppsFlags();

                // Initializing AppsBootstrapper.
                AppsBootstrapper.InitAppsBootstrapper(
                    (byte)schedulerCount,
                    configuration.DefaultUserHttpPort,
                    configuration.DefaultSystemHttpPort,
                    configuration.DefaultSessionTimeoutMinutes,
                    configuration.Name,
                    configuration.NoNetworkGateway);

                OnAppsBoostraperInitialized();

                // Configuring database related settings.
                if (withdb_) {
                    ConfigureDatabase(configuration);
                    OnDatabaseConfigured();

                    ConnectDatabase(configuration.InstanceID, schedulerCount, hlogs);
                    OnDatabaseConnected();
                }

                // Query module.
                Scheduler.Setup((Byte)schedulerCount);
                if (withdb_) {
                    Starcounter.Query.QueryModule.Initiate(
                        configuration.SQLProcessPort,
                        Path.Combine(configuration.TempDirectory, "sqlschemas"));

                    OnQueryModuleInitiated();
                }

                StarcounterBase.TransactionManager = new TransactionManager();

                return true;

            }
            finally { OnEndSetup(); }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private unsafe void Start() {
            try {

                uint e = sccorelib.cm2_start(hsched_);
                if (e != 0) throw ErrorCode.ToException(e);

                OnSchedulerStarted();

                // TODO: Fix the proper BMX push channel registration with gateway.
                // Waiting until BMX component is ready.
                if (!configuration.NoNetworkGateway) {
                    int ir = sccorelib.fix_wait_for_gateway_available(60000);
                    if (ir == 0)
                        throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED, "fix_wait_for_gateway_available didn't finish within given time interval.");

                    OnNetworkGatewayConnected();
                }

                var appDomain = AppDomain.CurrentDomain;
                appDomain.AssemblyResolve += new ResolveEventHandler(Loader.ResolveAssembly);

                OnAppDomainConfigured();

                ManagementService.Setup(configuration.DefaultSystemHttpPort, hsched_, !configuration.NoNetworkGateway);
                OnServerCommandHandlersRegistered();

                if (withdb_) {
                    Loader.AddBasePackage(hsched_, stopwatch_);
                    OnBasePackageLoaded();
                }

                // NOTE: Disabling skip for request filters since no more system handlers are expected at this line.
                StarcounterEnvironment.SkipRequestFiltersGlobal = false;
            }
            finally { OnEndStart(); }
        }

        /// <summary>
        /// Simple parser for user arguments.
        /// </summary>
        String[] ParseUserArguments(String userArgs) {
            char[] parmChars = userArgs.ToCharArray();
            bool inQuote = false;

            for (int i = 0; i < parmChars.Length; i++) {
                if (parmChars[i] == '"') {
                    parmChars[i] = '\n';
                    inQuote = !inQuote;
                }

                if (!inQuote && parmChars[i] == ' ')
                    parmChars[i] = '\n';
            }

            return (new string(parmChars)).Split(new Char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        private unsafe void Run() {
            try {

                // Executing auto-start task if any.
                if (configuration.AutoStartExePath != null) {
                    // Trying to get user arguments if any.
                    String userArgs = null;
                    String[] userArgsArray = null;
                    configuration.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.UserArguments, out userArgs);
                    if (userArgs != null) {
                        // Parsing user arguments.
                        String[] parsedUserArgs = ParseUserArguments(userArgs);

                        // Checking if any parameters determined.
                        if (parsedUserArgs.Length > 0)
                            userArgsArray = parsedUserArgs;
                    }

                    // Trying to get explicit working directory.
                    String workingDir = null;
                    configuration.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.WorkingDir, out workingDir);

                    OnArgumentsParsed();

                    var app = new ApplicationBase(
                        Path.GetFileName(configuration.AutoStartExePath),
                        configuration.AutoStartExePath,
                        configuration.AutoStartExePath,
                        workingDir,
                        userArgsArray
                    );
                    app.HostedFilePath = configuration.AutoStartExePath;
                    
                    // Loading the given application.
                    Loader.ExecuteApplication(hsched_, app, true, stopwatch_);

                    OnAutoStartModuleExecuted();
                }

                OnCodeHostBootCompleted();

                // Receive until we are told to shutdown.

                ManagementService.RunUntilShutdown();

            }
            finally { OnEndRun(); }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        private unsafe void Stop() {
            try {
                uint e = sccorelib.cm2_stop(hsched_, 1);
                if (e == 0) {
                    Db.RaiseDatabaseStoppingEvent();
                    return;
                }

                throw ErrorCode.ToException(e);
            }
            finally { OnEndStop(); }
        }

        /// <summary>
        /// Cleanups this instance.
        /// </summary>
        private void Cleanup() { OnEndCleanup(); }

        /// <summary>
        /// The process control_
        /// </summary>
        private System.Threading.EventWaitHandle processControl_;

        /// <summary>
        /// Assures the name of the no other process with the same.
        /// </summary>
        /// <param name="c">The c.</param>
        private void AssureNoOtherProcessWithTheSameName(Configuration c) {
            try {
                bool createdNew;
                processControl_ = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, c.Name, out createdNew);
                if (createdNew) return;
                processControl_.Dispose();
                processControl_ = null;
            }
            catch (UnauthorizedAccessException) {
                // Event exists but we can't access it. We treat it the same as
                // if the event exists and we can access it.
            }

            throw ErrorCode.ToException(Starcounter.Error.SCERRAPPALREADYSTARTED);
        }

        /// <summary>
        /// Calculates the amount of memory needed for runtime environment.
        /// </summary>
        /// <param name="schedulerCount">The scheduler count.</param>
        /// <returns>System.UInt32.</returns>
        private uint CalculateAmountOfMemoryNeededForRuntimeEnvironment(uint schedulerCount) {
            uint s =
                // Kernel memory setup. We actually only need 128 bytes but in
                // order for per scheduler memory to be aligned to page
                // boundary we allocate 512 bytes for this.

                512 + // 128

                // Scheduler: 1024 shared + 512 per scheduler.

                1024 +
                (schedulerCount * 512) +

                0;
            return s;
        }

        /// <summary>
        /// </summary>
        private unsafe ulong ConfigureLogging(Configuration c) {
            uint e;

            e = sccorelog.sccorelog_init();
            if (e != 0) throw ErrorCode.ToException(e);

            ulong hlogs;
            e = sccorelog.star_connect_to_logs(
                ScUri.MakeDatabaseUri(ScUri.GetMachineName(), c.ServerName, c.Name),
                c.OutputDirectory,
                null,
                &hlogs
                );
            if (e != 0) throw ErrorCode.ToException(e);

            LogManager.Setup(hlogs);
            return hlogs;
        }

        /// <summary>
        /// Configures the host.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> to use when
        /// configuring the host.</param>
        /// <param name="hlogs">The hlogs.</param>
        private unsafe void ConfigureHost(Configuration configuration, ulong hlogs) {
            uint e = sccoreapp.sccoreapp_init((void*)hlogs);
            if (e != 0) throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Configures the scheduler.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="mem">The mem.</param>
        /// <param name="hmenv">The hmenv.</param>
        /// <param name="schedulerCount">The scheduler count.</param>
        private unsafe void* ConfigureScheduler(Configuration c, void* mem, uint schedulerCount) {
            uint space_needed_for_scheduler = 1024 + (schedulerCount * 512);
            sccorelib.CM2_SETUP setup = new sccorelib.CM2_SETUP();
            setup.name = (char*)Marshal.StringToHGlobalUni(c.ServerName + "_" + c.Name);
            setup.server_name = (char*)Marshal.StringToHGlobalUni(c.ServerName);
            setup.db_data_dir_path = (char*)Marshal.StringToHGlobalUni(c.OutputDirectory); // TODO: ?
            setup.is_system = 0;
            setup.num_shm_chunks = c.ChunksNumber;
            setup.gateway_num_workers = c.GatewayNumberOfWorkers;
            setup.mem = mem;
            setup.mem_size = space_needed_for_scheduler;
            setup.cpuc = (byte)schedulerCount;
            if (withdb_) orange.orange_configure_scheduler_callbacks(ref setup);
            else orange_nodb.orange_configure_scheduler_callbacks(ref setup);

            void* hsched;
            uint r = sccorelib.cm2_setup(&setup, &hsched);

            Marshal.FreeHGlobal((IntPtr)setup.name);
            Marshal.FreeHGlobal((IntPtr)setup.server_name);
            Marshal.FreeHGlobal((IntPtr)setup.db_data_dir_path);

            if (r == 0) {
                Processor.Setup(hsched);
                return hsched;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Configures the database.
        /// </summary>
        /// <param name="c">The c.</param>
        private unsafe void ConfigureDatabase(Configuration c) {
            uint e;

            var callbacks = new sccoredb.sccoredb_callbacks();
            orange.orange_configure_database_callbacks(ref callbacks);
            e = sccoredb.star_set_system_callbacks(&callbacks);
            if (e != 0) throw ErrorCode.ToException(e);
        }

        private static void ProcessCallbackMessagesThread(Object parameters) {
            ulong hlogs = (ulong)parameters;
            synccommit.star_process_callback_messages(hlogs);
        }

        /// <summary>
        /// </summary>
        private unsafe void ConnectDatabase(uint instanceId, uint schedulerCount, ulong hlogs) {
            uint e = sccoredb.sccoredb_connect(instanceId, schedulerCount, hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            // Start thread to process asynchronous messages from data manager. Required to handle
            // synchronous commits, configuration change broadcasts and data manager failure
            // detection.

            var t = new System.Threading.Thread(ProcessCallbackMessagesThread);
            t.IsBackground = true;
            t.Start(hlogs);
        }

        private long ticksElapsedBetweenProcessStartAndMain_;
        private Stopwatch stopwatch_;

        [Conditional("TRACE")]
        private void Trace(string message, bool restartWatch = false) {
            if (restartWatch) {
                stopwatch_.Restart();
                ticksElapsedBetweenProcessStartAndMain_ = 0;
            }
            long elapsedTicks = stopwatch_.ElapsedTicks + ticksElapsedBetweenProcessStartAndMain_;
            Diagnostics.WriteTrace(log.Source, elapsedTicks, message);
            Diagnostics.WriteTimeStamp(log.Source, message);
        }

        private void OnProcessInitialized() {
            ticksElapsedBetweenProcessStartAndMain_ = (DateTime.Now - Process.GetCurrentProcess().StartTime).Ticks;
            stopwatch_ = Stopwatch.StartNew();

            Trace("Bootstrap Main() started.");
        }

        private void OnExceptionFactoryInstalled() { Trace("Exception factory installed."); }
        private void OnCommandLineParsed() { Trace("Command line parsed."); }
        private void OnConfigurationLoaded() { Trace("Configuration loaded."); }
        private void OnAssuredNoOtherProcessWithTheSameName() { Trace("Assured no other process with the same name."); }
        private void OnGlobalMemoryAllocated() { Trace("Global memory allocated."); }
        private void OnBmxManagerInitialized() { Trace("BMX manager initialized."); }
        private void OnLoggingConfigured() { Trace("Logging configured."); }
        private void OnHostConfigured() { Trace("Host configured."); }
        private void OnSchedulerConfigured() { Trace("Scheduler configured."); }
        private void OnDatabaseConnected() { Trace("Database connected."); }
        private void OnAppsBoostraperInitialized() { Trace("Apps bootstraper initialized."); }
        private void OnDatabaseConfigured() { Trace("Database configured."); }
        private void OnQueryModuleInitiated() { Trace("Query module initiated."); }

        private void OnEndSetup() { Trace("Setup completed."); }

        private void OnSchedulerStarted() { Trace("Scheduler started."); }
        private void OnAppDomainConfigured() { Trace("App domain configured."); }
        private void OnServerCommandHandlersRegistered() { Trace("Server command handlers registered."); }
        private void OnBasePackageLoaded() { Trace("Base package loaded."); }
        private void OnNetworkGatewayConnected() { Trace("Network gateway connected."); }
        private void OnArgumentsParsed() { Trace("Command line arguments parsed."); }
        private void OnAutoStartModuleExecuted() { Trace("Auto start module executed."); }
        private void OnCodeHostBootCompleted() { Trace("Booting completed."); }

        private void OnEndStart() { Trace("Start completed."); }

        private void OnEndRun() { Trace("Run completed.", true); }
        private void OnEndStop() { Trace("Stop completed."); }
        private void OnEndCleanup() { Trace("Cleanup completed."); }
    }
}
