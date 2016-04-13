// ***********************************************************************
// <copyright file="Loader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Sc.Server.Weaver.Schema;
using Starcounter;
using Starcounter.Hosting;
using Starcounter.Internal;
using Starcounter.Metadata;
using System.Threading;
using Starcounter.Logging;

namespace StarcounterInternal.Hosting
{
    /// <summary>
    /// Class LoaderException
    /// </summary>
    public class LoaderException : Exception
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public LoaderException(string message) : base(message) { }
    }

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
        /// Adds the base package.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        public static unsafe void AddBasePackage(void* hsched, Stopwatch stopwatch = null)
        {
            if (stopwatch != null)
                stopwatch_ = stopwatch;
            else
                stopwatch_ = new Stopwatch();

            var sysTableTypeDef = Starcounter.Internal.Metadata.MaterializedTable.CreateTypeDef();
            var sysColumnTypeDef = Starcounter.Internal.Metadata.MaterializedColumn.CreateTypeDef();
            var sysIndexTypeDef = Starcounter.Internal.Metadata.MaterializedIndex.CreateTypeDef();
            var sysIndexColumnTypeDef = Starcounter.Internal.Metadata.MaterializedIndexColumn.CreateTypeDef();

            // Add view meta-data

            Package package = new Package(
                new TypeDef[] { sysTableTypeDef, sysColumnTypeDef, sysIndexTypeDef, sysIndexColumnTypeDef,
                    Starcounter.Metadata.Type.CreateTypeDef(), Starcounter.Metadata.DbPrimitiveType.CreateTypeDef(), 
                    Starcounter.Metadata.MapPrimitiveType.CreateTypeDef(), ClrPrimitiveType.CreateTypeDef(),
                    Table.CreateTypeDef(), RawView.CreateTypeDef(),
                    VMView.CreateTypeDef(), ClrClass.CreateTypeDef(), 
                    Member.CreateTypeDef(), Column.CreateTypeDef(), 
                    Property.CreateTypeDef(), CodeProperty.CreateTypeDef(), MappedProperty.CreateTypeDef(),
                    Index.CreateTypeDef(), IndexedColumn.CreateTypeDef()
                },
                stopwatch_
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

            var filePath = application.HostedFilePath;
            try {
                filePath = filePath.Trim('\"', '\\');
                filePath = Path.GetFullPath(filePath);
            } catch (ArgumentException pathEx) {
                throw new LoaderException(string.Format("{0} ({1})", pathEx.Message, filePath));
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                throw new LoaderException(string.Format("File not found: {0}.", filePath));
            }

            var inputFile = new FileInfo(filePath);

            assemblyResolver.PrivateAssemblies.RegisterApplicationDirectory(inputFile.Directory);

            OnInputVerifiedAndAssemblyResolverUpdated();

            var typeDefs = SchemaLoader.LoadAndConvertSchema(inputFile.Directory);

            OnSchemaVerifiedAndLoaded();

            var assembly = assemblyResolver.ResolveApplication(inputFile.FullName);
            if (assembly.EntryPoint == null) {
                throw ErrorCode.ToException(Starcounter.Error.SCERRAPPLICATIONNOTANEXECUTABLE, string.Format("Failing application: {0}", inputFile.FullName));
            }

            OnTargetAssemblyLoaded();

            var package = new Package(
                typeDefs.ToArray(),
                stopwatch_,
                assembly,
                application,
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
        private static void OnInputVerifiedAndAssemblyResolverUpdated() { Trace("Input verified and assembly resolver updated."); }
        private static void OnSchemaVerifiedAndLoaded() { Trace("Schema verified and loaded."); }
        private static void OnTargetAssemblyLoaded() { Trace("Target assembly loaded."); }
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
