// ***********************************************************************
// <copyright file="Loader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Hosting;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Metadata;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace StarcounterInternal.Hosting {
    /// <summary>
    /// Class Loader
    /// </summary>
    public static class Loader
    {
        static AssemblyResolver assemblyResolver = new AssemblyResolver(new PrivateAssemblyStore());

        internal static LogSource Log = LogSources.CodeHostLoader;

        [ThreadStatic]
        private static Stopwatch stopwatch_;

        internal static AssemblyResolver Resolver {
            get {
                return assemblyResolver;
            }
        }

        /// <summary>
        /// Resolves the assembly.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ResolveEventArgs" /> instance containing the event data.</param>
        /// <returns>Assembly.</returns>
        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            return assemblyResolver.ResolveApplicationReference(args);
        }

        /// <summary>
        /// Adds the package defined by Starcounter, part of every code host
        /// and always loaded first.
        /// </summary>
        /// <param name="hsched">Pointer to scheduler.</param>
        /// <param name="stopwatch">Optional stop watch</param>
        public static unsafe void AddBasePackage(void* hsched, Stopwatch stopwatch = null)
        {
            if (stopwatch != null)
                stopwatch_ = stopwatch;
            else
                stopwatch_ = new Stopwatch();

            var package = StarcounterPackage.Create(stopwatch_);
            IntPtr hPackage = (IntPtr)GCHandle.Alloc(package, GCHandleType.Normal);

            OnPackageCreated();

            uint e = sccorelib.cm2_schedule(
                hsched,
                0,
                sccorelib_ext.TYPE_PROCESS_PACKAGE,
                0,
                0,
                0,
                (ulong)hPackage
                );
            if (e != 0) throw ErrorCode.ToException(e);

            // We only process one package at a time. Wait for the package
            // to be processed before accepting more input.
            //
            // (We can only handle one package at a time or we can not
            // evaluate if a type definition has already been loaded.)

            package.WaitUntilProcessed();
            package.Dispose();

            OnPackageProcessed();

            stopwatch_ = null;
        }

        /// <summary>
        /// Executes an application in the code host.
        /// </summary>
        /// <param name="hsched">Handle to the environment.</param>
        /// <param name="appBase">The application to execute.</param>
        /// <param name="execEntryPointSynchronously">Indicates if the entrypoint
        /// should be executed synchrounously, i.e. before this method return.</param>
        /// <param name="stopwatch">An optional stopwatch to use for timing.</param>
        public static unsafe void ExecuteApplication(
            void* hsched,
            ApplicationBase appBase,
            bool execEntryPointSynchronously = false,
            Stopwatch stopwatch = null) {

            stopwatch_ = stopwatch ?? Stopwatch.StartNew();
            OnLoaderStarted();

            var application = new Application(appBase, DefaultHost.Current);
            var inputFile = new FileInfo(application.HostedFilePath);
            var appDir = new ApplicationDirectory(inputFile.Directory);

            var typeDefs = SchemaLoader.LoadAndConvertSchema(inputFile.Directory);
            OnSchemaVerifiedAndLoaded();

            var package = new Package(
                typeDefs.ToArray(),
                stopwatch_,
                application,
                appDir,
                execEntryPointSynchronously
            );

            IntPtr hPackage = (IntPtr)GCHandle.Alloc(package, GCHandleType.Normal);

            OnPackageCreated();

            uint e = sccorelib.cm2_schedule(
                hsched,
                0,
                sccorelib_ext.TYPE_PROCESS_PACKAGE,
                0,
                0,
                0,
                (ulong)hPackage
                );
            if (e != 0) throw ErrorCode.ToException(e);

            // We only process one package at a time. Wait for the package
            // to be processed before accepting more input.
            //
            // (We can only handle one package at a time or we can not
            // evaluate if a type definition has already been loaded.)

            try {
                var result = package.WaitUntilProcessed();
                package.Dispose();
                if (result != 0) {
                    // The package didn't process successfully, which
                    // currently means that the thread processing it should
                    // assure the process is shut down with the failure
                    // being logged.
                    //   Our strategy is therefore to await this to happen.
                    // If it doesnt in some time, we raise an exception our
                    // self, which will bring the process down instead.
                    Thread.Sleep(2000);
                    throw ErrorCode.ToException(result);
                }

                OnPackageProcessed();

            } finally {
                stopwatch_ = null;
            }
        }
        
        private static void OnLoaderStarted() { Trace("Loader started."); }
        private static void OnSchemaVerifiedAndLoaded() { Trace("Schema verified and loaded."); }
        private static void OnPackageCreated() { Trace("Package created."); }
        private static void OnPackageProcessed() { Trace("Package processed."); }

        [Conditional("TRACE")]
        private static void Trace(string message)
        {
            Diagnostics.WriteTrace(Log.Source, stopwatch_.ElapsedTicks, message);

            Diagnostics.WriteTimeStamp(Log.Source, message);
        }
    }
}
