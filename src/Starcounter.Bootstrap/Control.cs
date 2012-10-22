// ***********************************************************************
// <copyright file="Control.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using Starcounter.CommandLine;
using Starcounter; // TODO:
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal; // TODO:
using Starcounter.Logging;
using StarcounterInternal.Hosting;
using Error = Starcounter.Internal.Error;

namespace StarcounterInternal.Bootstrap
{
    /// <summary>
    /// Class Control
    /// </summary>
    public class Control // TODO: Make internal.
    {
        // Loaded configuration info.
        /// <summary>
        /// The configuration
        /// </summary>
        Configuration configuration = null;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args)
        {
            try
            {
                Control c = new Control();
                if (c.Setup(args))
                {
                    c.Start();
                    c.Run();
                    c.Stop();
                    c.Cleanup();
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        /// <summary>
        /// The withdb_
        /// </summary>
        private bool withdb_;
        /// <summary>
        /// The hsched_
        /// </summary>
        private unsafe void* hsched_;

        /// <summary>
        /// The <see cref="Server"/> used as the interface to support local
        /// requests such as the hosting/exeuting of executables and to handle
        /// our servers management demands.
        /// </summary>
        /// <see cref="Control.ConfigureHost"/>
        private Server server;

        /// <summary>
        /// Setups the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private unsafe bool Setup(string[] args)
        {
#if false
            // Disables priority boost for all the threads in the process.
            // Often a good idea when using spin-locks. Not sure it worth
            // anything with the current setup however since most often no more
            // running threads then cores. So leaving this disabled for now.

            Kernel32.SetProcessPriorityBoost(Kernel32.GetCurrentProcess(), 1);
#endif

            DatabaseExceptionFactory.InstallInCurrentAppDomain();

            ApplicationArguments arguments;
            if (!ProgramCommandLine.TryGetProgramArguments(args, out arguments))
                return false;

            configuration = Configuration.Load(arguments);

            withdb_ = !configuration.NoDb;

            AssureNoOtherProcessWithTheSameName(configuration);

            uint schedulerCount = configuration.SchedulerCount;

            uint memSize = CalculateAmountOfMemoryNeededForRuntimeEnvironment(schedulerCount);

            byte* mem = (byte*)Kernel32.VirtualAlloc((void *)0, (IntPtr)memSize, Kernel32.MEM_COMMIT, Kernel32.PAGE_READWRITE);

            // Note that we really only need 128 bytes. See method
            // CalculateAmountOfMemoryNeededForRuntimeEnvironment for details.

            ulong hmenv = ConfigureMemory(configuration, mem);
            mem += 512;

            // Initializing the BMX manager if we have network applications.
            if (configuration.NetworkApps)
                bmx.sc_init_bmx_manager();

            ulong hlogs = ConfigureLogging(configuration, hmenv);

            ConfigureHost(configuration, hlogs);

            hsched_ = ConfigureScheduler(configuration, mem, hmenv, schedulerCount);
            mem += (1024 + (schedulerCount * 512));

            if (withdb_)
            {
                ConfigureDatabase(configuration);
                ConnectDatabase(configuration, hsched_, hmenv, hlogs);
            }

            // Query module.
            Scheduler.Setup((byte)schedulerCount);
            if (withdb_)
            {
                Starcounter.Query.QueryModule.Initiate(configuration.SQLProcessPort);
            }

            return true;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private unsafe void Start()
        {
            uint e = sccorelib.cm2_start(hsched_);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        private unsafe void Run()
        {
            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += new ResolveEventHandler(Loader.ResolveAssembly);

            // Install handlers for the type of requests we accept.

            // Handles execution requests for executables that support
            // lauching into Starcounter from the OS shell.
            server.Handle("Exec", delegate(Request r)
            {
                try
                {
                    Loader.ExecApp(hsched_, r.GetParameter<string>());
                }
                catch (LoaderException ex)
                {
                    r.Respond(false, ex.Message);
                }
            });

            // Ping, allowing clients to check the responsiveness of the
            // code host.

            server.Handle("Ping", delegate(Request request)
            {
                request.Respond(true);
            });

            if (withdb_) Loader.AddBasePackage(hsched_);

            // TODO: Fix the proper BMX push channel registration with gateway.
            // Waiting until BMX component is ready.
            if (configuration.NetworkApps)
                bmx.sc_wait_for_bmx_ready();

            // Executing auto-start task if any.
            if (configuration.AutoStartExePath != null)
            {
                // Loading the given application.
                Loader.ExecApp(hsched_, configuration.AutoStartExePath);
            }

            // Receive until we are told to shutdown.

            server.Receive();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        private unsafe void Stop()
        {
            uint e = sccorelib.cm2_stop(hsched_, 1);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Cleanups this instance.
        /// </summary>
        private void Cleanup()
        {
            DisconnectDatabase();
        }

        /// <summary>
        /// The process control_
        /// </summary>
        private System.Threading.EventWaitHandle processControl_;

        /// <summary>
        /// Assures the name of the no other process with the same.
        /// </summary>
        /// <param name="c">The c.</param>
        private void AssureNoOtherProcessWithTheSameName(Configuration c)
        {
            try
            {
                bool createdNew;
                processControl_ = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, c.Name, out createdNew);
                if (createdNew) return;
                processControl_.Dispose();
                processControl_ = null;
            }
            catch (UnauthorizedAccessException)
            {
                // Event exists but we can't access it. We treat it the same as
                // if the event exists and we can access it.
            }

            throw ErrorCode.ToException(Error.SCERRAPPALREADYSTARTED);
        }

        /// <summary>
        /// Calculates the amount of memory needed for runtime environment.
        /// </summary>
        /// <param name="schedulerCount">The scheduler count.</param>
        /// <returns>System.UInt32.</returns>
        private uint CalculateAmountOfMemoryNeededForRuntimeEnvironment(uint schedulerCount)
        {
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
        /// Configures the memory.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="mem128">The mem128.</param>
        /// <returns>System.UInt64.</returns>
        private unsafe ulong ConfigureMemory(Configuration c, void* mem128)
        {
            uint slabs = (0xFFFFF000 - 4096) / 4096;  // 4 GB - 4 KB
            ulong hmenv = sccorelib.mh4_menv_create(mem128, slabs);
            if (hmenv != 0) return hmenv;
            throw ErrorCode.ToException(Error.SCERROUTOFMEMORY);
        }

        /// <summary>
        /// Configures the logging.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="hmenv">The hmenv.</param>
        /// <returns>System.UInt64.</returns>
        private unsafe ulong ConfigureLogging(Configuration c, ulong hmenv)
        {
            uint e;

            e = sccorelog.SCInitModule_LOG(hmenv);
            if (e != 0) throw ErrorCode.ToException(e);

            ulong hlogs;
            e = sccorelog.SCConnectToLogs(c.Name, null, null, &hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccorelog.SCBindLogsToDir(hlogs, c.OutputDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            return hlogs;
        }

        /// <summary>
        /// Configures the host.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> to use when
        /// configuring the host.</param>
        /// <param name="hlogs">The hlogs.</param>
        private unsafe void ConfigureHost(Configuration configuration, ulong hlogs)
        {
            uint e = sccoreapp.sccoreapp_init((void*)hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            LogManager.Setup(hlogs);

            // Decide what interface to expose locally, to handle requests
            // from the server and from executables being loaded from the
            // shell.
            //   Currently, named pipes is the standard means.

            if (!configuration.UseConsole) {
                var pipeName = string.Format("sc//{0}/{1}/{2}", Environment.MachineName, configuration.ServerName, configuration.Name);
                pipeName = pipeName.ToLowerInvariant();
                server = ClientServerFactory.CreateServerUsingNamedPipes(pipeName);

            } else {
                // Expose services via standard streams.
                //
                // If input has not been redirected, we let the server accept
                // requests in a simple text format from the console.
                // 
                // If the input has been redirected, we force the parent process
                // to use the "real" API's (i.e. the Client) and expose our
                // services "raw" on the standard streams.

                if (!Console.IsInputRedirected) {
                    server = ClientServerFactory.CreateServerUsingConsole();
                } else {
                    server = new Server(Console.In.ReadLine, (string reply, bool endsRequest) => {
                        Console.Out.WriteLine(reply);
                    });
                }
            }
        }

        /// <summary>
        /// Configures the scheduler.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="mem">The mem.</param>
        /// <param name="hmenv">The hmenv.</param>
        /// <param name="schedulerCount">The scheduler count.</param>
        private unsafe void* ConfigureScheduler(Configuration c, void* mem, ulong hmenv, uint schedulerCount)
        {
            if (withdb_) orange.orange_setup(hmenv);
            else orange_nodb.orange_setup(hmenv);

            uint space_needed_for_scheduler = 1024 + (schedulerCount * 512);
            sccorelib.CM2_SETUP setup = new sccorelib.CM2_SETUP();
            setup.name = (char*)Marshal.StringToHGlobalUni(c.ServerName + "_" + c.Name);
            setup.server_name = (char*)Marshal.StringToHGlobalUni(c.ServerName);
            setup.db_data_dir_path = (char*)Marshal.StringToHGlobalUni(c.OutputDirectory); // TODO: ?
            setup.is_system = 0;
            setup.num_shm_chunks = c.ChunksNumber;
            setup.mem = mem;
            setup.mem_size = space_needed_for_scheduler;
            setup.hmenv = hmenv;
            setup.cpuc = (byte)schedulerCount;
            if (withdb_) orange.orange_configure_scheduler_callbacks(ref setup);
            else orange_nodb.orange_configure_scheduler_callbacks(ref setup);

            void* hsched;
            uint e = sccorelib.cm2_setup(&setup, &hsched);

            Marshal.FreeHGlobal((IntPtr)setup.name);
            Marshal.FreeHGlobal((IntPtr)setup.server_name);
            Marshal.FreeHGlobal((IntPtr)setup.db_data_dir_path);

            if (e == 0) return hsched;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Configures the database.
        /// </summary>
        /// <param name="c">The c.</param>
        private unsafe void ConfigureDatabase(Configuration c)
        {
            uint e;

            e = sccoredb.SCConfigSetValue("NAME", c.Name);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("IMAGEDIR", c.DatabaseDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("OLOGDIR", c.DatabaseDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("TLOGDIR", c.DatabaseDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("TEMPDIR", c.TempDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("COMPPATH", c.CompilerPath);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("OUTDIR", c.OutputDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            // TODO: What is this configuration for?
            e = sccoredb.SCConfigSetValue("ELOGDIR", c.OutputDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            sccoredb.sccoredb_config config = new sccoredb.sccoredb_config();
            orange.orange_configure_database_callbacks(ref config);
            e = sccoredb.sccoredb_configure(&config);
            if (e != 0) throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Connects the database.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="hsched">The hsched.</param>
        /// <param name="hmenv">The hmenv.</param>
        /// <param name="hlogs">The hlogs.</param>
        private unsafe void ConnectDatabase(Configuration configuration, void* hsched, ulong hmenv, ulong hlogs)
        {
            uint e;

            uint flags = 0;
            flags |= sccoredb.SCCOREDB_LOAD_DATABASE;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD;
            //flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP;

            // Temporary solution. See flag docs for details.
            flags |= sccoredb.SCCOREDB_COMPLETE_INIT;

            int empty;
            e = sccoredb.sccoredb_connect(flags, hsched, hmenv, hlogs, &empty);
            if (e != 0) throw ErrorCode.ToException(e);
        }


        /// <summary>
        /// Disconnects the database.
        /// </summary>
        private void DisconnectDatabase()
        {
            uint e = sccoredb.sccoredb_disconnect(0);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }
    }
}
