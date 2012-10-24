// ***********************************************************************
// <copyright file="App.ExeModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.ExeModule;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class App {

#if !CLIENT


        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void GET(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void GET<T>(string uri, Func<T, object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        /// <summary>
        /// GETs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void GET<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.GET(uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void PUT(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void PUT<T1>(string uri, Func<T1, object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        /// <summary>
        /// PUTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void PUT<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.PUT(uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void POST(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void POST<T1>(string uri, Func<T1, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void POST<T1, T2>(string uri, Func<T1, T2, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        /// <summary>
        /// POSTs the specified URI.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <typeparam name="T3">The type of the t3.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void POST<T1, T2, T3>(string uri, Func<T1, T2, T3, object> handler) {
            CheckProcess();
            RequestHandler.POST(uri, handler);
        }
        /// <summary>
        /// DELETEs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void DELETE(string uri, Func<object> handler) {
            CheckProcess();
            RequestHandler.DELETE(uri, handler);
        }
        /// <summary>
        /// PATCHs the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="handler">The handler.</param>
        public static new void PATCH(string uri, Func<string, object> handler)
        {
            CheckProcess();
            RequestHandler.PATCH(uri, handler);
        }

        /// <summary>
        /// The has started
        /// </summary>
        [ThreadStatic]
        private static bool hasStarted = false;

        /// <summary>
        /// Occurs when [on started].
        /// </summary>
        public static event Action OnStarted;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has started.
        /// </summary>
        /// <value><c>true</c> if this instance has started; otherwise, <c>false</c>.</value>
        /// <exception cref="System.Exception">You cannot set HasStarted to false</exception>
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

        /// <summary>
        /// The already checked process
        /// </summary>
        private static bool AlreadyCheckedProcess = false;
        /// <summary>
        /// Initializes static members of the <see cref="App" /> class.
        /// </summary>
        static App() {
            CheckProcess();
        }

        /// <summary>
        /// Checks the process.
        /// </summary>
        internal static void CheckProcess() {
            if (AlreadyCheckedProcess)
                return;
            AlreadyCheckedProcess = true;
            Console.WriteLine("Checking that the process is running inside Starcounter hosts.");
            if (!AppExeModule.IsRunningTests)
            {
                Process pr = Process.GetCurrentProcess();
                string pn = pr.ProcessName;
                if (pn != "boot" && pn != "AppsStarterMsSql" && pn != "AppsStarterMsSql.vshost" && pn != "scdbs" && pn != "scdbsw" && pn != "Fakeway" && pn != "Fakeway.vshost")
                {
                    // TODO! Is this code still operational? TellServer does not do anything anymore.
                    Console.WriteLine();
                    Console.WriteLine(pn);
                    Console.WriteLine("Restarting this .EXE in the cloud.");
                    TellServerToStartMe.TellServer();
                    System.Environment.Exit(123);
                }
                Console.WriteLine("Started this .EXE inside " + pn);
            }
        }

        /// <summary>
        /// Gets a value indicating whether [admin wants to stop].
        /// </summary>
        /// <value><c>true</c> if [admin wants to stop]; otherwise, <c>false</c>.</value>
        public static bool AdminWantsToStop {
            get { return WantsToStop(); }
        }

        /// <summary>
        /// The wants to stop
        /// </summary>
        public static Func<bool> WantsToStop;
        /// <summary>
        /// The run action
        /// </summary>
        public static Action<int> RunAction;

        /// <summary>
        /// Runs the specified ticks.
        /// </summary>
        /// <param name="ticks">The ticks.</param>
        /// <exception cref="System.Exception">The bootstrapper has not assigned a RunAction for the user App classes.</exception>
        public static void Run(int ticks = 1000) {
            HasStarted = true;
            if (RunAction == null)
                throw new Exception("The bootstrapper has not assigned a RunAction for the user App classes.");
            RunAction(ticks);
        }
#endif
    }
}
