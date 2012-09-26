
using System;
using System.Runtime.InteropServices;
using Starcounter.CommandLine;
using Starcounter; // TODO:
using Starcounter.Internal; // TODO:
using StarcounterInternal.Hosting;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;

namespace StarcounterInternal.Bootstrap
{

    public class Control // TODO: Make internal.
    {

        public static void Main(string[] args)
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

        private unsafe void* hsched_;

        private unsafe bool Setup(string[] args)
        {
            ApplicationArguments arguments;

            DatabaseExceptionFactory.InstallInCurrentAppDomain();

            if (!ProgramCommandLine.TryGetProgramArguments(args, out arguments))
                return false;

            Configuration configuration = Configuration.Load(arguments);

#if false // TODO:
            _SetCriticalLogHandler(_LogCritical, NULL);
            SetUnhandledExceptionFilter(_UnhandledExceptionFilter);
            AddVectoredExceptionHandler(0, _VectoredExceptionHandler);
#endif
            // TODO:
#if false // TODO:
            br = SetProcessPriorityBoost(GetCurrentProcess(), TRUE);
            _ASSERT(br != FALSE);
#endif

#if false
	        wcscpy_s(temp, (13 + 1), L"Global\\SCAPP_");
	        wcscpy_s((temp + 13), (_MAX_APPNAME_LENGTH + 1), serverName);
	        he = CreateEvent(NULL, TRUE, FALSE, temp);
	        if (he == NULL)
	        {
		        dr = SCERROUTOFMEMORY;
		        goto start_err;
	        }
	        dr = GetLastError();
	        if (dr == ERROR_ALREADY_EXISTS)
	        {
		        dr = SCERRAPPALREADYSTARTED;
		        goto start_err;
	        }
	        _pGlobals->hEvent = he;
#endif

            byte* mem = (byte*)Marshal.AllocHGlobal(4096); // TODO: Allocate aligned memory. Evaluate size.

            ulong hmenv = ConfigureMemory(configuration, mem);
            mem += 128;

            ulong hlogs = ConfigureLogging(configuration, hmenv);

            hsched_ = ConfigureScheduler(configuration, mem, hmenv);
            mem += (1024 + 512);

            ConfigureDatabase(configuration);

            ConnectDatabase(configuration, hsched_, hmenv, hlogs);

            // Query module.
            Scheduler.Setup(1);

            return true;
        }

        private unsafe void Start()
        {
            uint e = sccorelib.cm2_start(hsched_);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        private unsafe void Run()
        {
            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += new ResolveEventHandler(Loader.ResolveAssembly);
            Server server;

            // Create the server.
            // If input has not been redirected, we let the server accept
            // requests in a simple text format from the console.
            // 
            // If the input has been redirected, we force the parent process
            // to use the "real" API's (i.e. the Client), just as the server
            // will do, once it has been moved into Orange.

            if (!Console.IsInputRedirected)
            {
                server = Utils.PromptHelper.CreateServerAttachedToPrompt();
            }
            else
            {
                server = new Server(Console.In.ReadLine, Console.Out.WriteLine);
            }

            // Install handlers for the type of requests we accept.

            // Handles execution requests for Apps
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

            // Some test handlers to show a little more.
            // To be removed.

            server.Handle("Ping", delegate(Request request)
            {
                request.Respond(true);
            });

            server.Handle("Echo", delegate(Request request)
            {
                var response = request.GetParameter<string>();
                request.Respond(response ?? "<NULL>");
            });

            // Receive until we are told to shutdown

            server.Receive();
        }

        private unsafe void Stop()
        {
            uint e = sccorelib.cm2_stop(hsched_, 1);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        private void Cleanup()
        {
            DisconnectDatabase();
        }


        private unsafe ulong ConfigureMemory(Configuration c, void* mem128)
        {
            uint slabs = (0xFFFFF000 - 4096) / 4096;  // 4 GB - 4 KB
            ulong hmenv = sccorelib.mh4_menv_create(mem128, slabs);
            if (hmenv != 0) return hmenv;
            throw ErrorCode.ToException(Error.SCERROUTOFMEMORY);
        }

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

        private unsafe void* ConfigureScheduler(Configuration c, void* mem, ulong hmenv)
        {
            uint cpuc = 1;

            orange.orange_setup(hmenv);

            uint space_needed_for_scheduler = 1024 + (cpuc * 512);
            sccorelib.CM2_SETUP setup = new sccorelib.CM2_SETUP();
            setup.name = (char*)Marshal.StringToHGlobalUni(c.Name);
            setup.server_name = (char*)Marshal.StringToHGlobalUni("PERSONAL");
            setup.db_data_dir_path = (char*)Marshal.StringToHGlobalUni(c.OutputDirectory); // TODO: ?
            setup.is_system = 0;
            setup.mem = mem;
            setup.mem_size = space_needed_for_scheduler;
            setup.hmenv = hmenv;
            setup.cpuc = (byte)cpuc;
            orange.orange_configure_scheduler_callbacks(ref setup);

            void* hsched;
            uint e = sccorelib.cm2_setup(&setup, &hsched);

            Marshal.FreeHGlobal((IntPtr)setup.name);
            Marshal.FreeHGlobal((IntPtr)setup.server_name);
            Marshal.FreeHGlobal((IntPtr)setup.db_data_dir_path);

            if (e == 0) return hsched;
            throw ErrorCode.ToException(e);
        }

        private unsafe void ConfigureDatabase(Configuration c)
        {
            uint e;

            e = sccoredb.SCConfigSetValue("NAME", c.Name);
            if (e != 0) throw ErrorCode.ToException(e);

            e = sccoredb.SCConfigSetValue("LONGNAME", c.Name);
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
        }

        private unsafe void ConnectDatabase(Configuration configuration, void* hsched, ulong hmenv, ulong hlogs)
        {
            uint e;

            uint flags = 0;
            flags |= sccoredb.SCCOREDB_LOAD_DATABASE;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD;
            //            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP;
            flags |= sccoredb.SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP;

            // Temporary solution. See flag docs for details.
            flags |= sccoredb.SCCOREDB_COMPLETE_INIT;

            int empty;
            e = sccoredb.sccoredb_connect(flags, hsched, hmenv, hlogs, &empty);
            if (e != 0) throw ErrorCode.ToException(e);
        }


        private void DisconnectDatabase()
        {
            uint e = sccoredb.sccoredb_disconnect(0);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }
    }
}
