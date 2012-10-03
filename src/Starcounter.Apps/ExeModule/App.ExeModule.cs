
using System;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.ExeModule;

namespace Starcounter {
    public partial class App {

#if !CLIENT


        public static new void GET(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        public static new void GET<T>(string uri, Func<T, object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        public static new void GET<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        public static new void PUT(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        public static new void PUT<T1>(string uri, Func<T1, object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        public static new void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        public static new void POST(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        public static new void POST<T1>(string uri, Func<T1, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        public static new void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        public static new void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        public static new void DELETE(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.DELETE(uri, handler);
        }
        public static new void PATCH(string uri, Func<string, object> handler)
        {
            CheckProcess();
            RequestHandler.PATCH(uri, handler);
        }

        [ThreadStatic]
        private static bool hasStarted = false;

        public static event Action OnStarted;

        public static bool HasStarted {
            get {
                return hasStarted;
            }
            set {
                if (!value)
                    throw new Exception("You cannot set HasStarted to false");
                if (!hasStarted) {
                    hasStarted = true;
                    if (OnStarted != null)
                        OnStarted();
                }
            }
        }

        static App() {
            CheckProcess();
        }

        public static void CheckProcess() {
            Console.WriteLine("Inside App type initializer");
            if (!AppExeModule.IsRunningTests)
            {
                Console.WriteLine("Checking process");
                Process pr = Process.GetCurrentProcess();
                string pn = pr.ProcessName;
                if (pn != "boot" && pn != "AppsStarterMsSql" && pn != "AppsStarterMsSql.vshost" && pn != "scdbs" && pn != "scdbsw" && pn != "Fakeway" && pn != "Fakeway.vshost")
                {
                    Console.WriteLine();
                    Console.WriteLine(pn);
                    Console.WriteLine("Restarting this .EXE in the cloud.");
                    TellServerToStartMe.TellServer();
                    System.Environment.Exit(123);
                }
                Console.WriteLine("Started this .EXE inside " + pn);
            }
        }

        public static bool AdminWantsToStop {
            get { return WantsToStop(); }
        }

        public static Func<bool> WantsToStop;
        public static Action<int> RunAction;

        public static void Run(int ticks = 1000) {
            HasStarted = true;
            if (RunAction == null)
                throw new Exception("The bootstrapper has not assigned a RunAction for the user App classes.");
            RunAction(ticks);
        }
#endif
    }
}
