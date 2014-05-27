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

        [ThreadStatic]
        private static Stopwatch stopwatch_;

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

            var sysTableTypeDef = MaterializedTable.CreateTypeDef();
            var sysColumnTypeDef = MaterializedColumn.CreateTypeDef();
            var sysIndexTypeDef = MaterializedIndex.CreateTypeDef();
            var sysIndexColumnTypeDef = MaterializedIndexColumn.CreateTypeDef();

            // Add view meta-data

            Package package = new Package(
                new TypeDef[] { sysTableTypeDef, sysColumnTypeDef, sysIndexTypeDef, sysIndexColumnTypeDef,
                    Starcounter.Metadata.Type.CreateTypeDef(), MaterializedType.CreateTypeDef(), 
                    MappedType.CreateTypeDef(), ClrPrimitiveType.CreateTypeDef(),
                    Table.CreateTypeDef(), HostMaterializedTable.CreateTypeDef(), RawView.CreateTypeDef(),
                    VMView.CreateTypeDef(), ClrClass.CreateTypeDef(), Member.CreateTypeDef(), 
                    Column.CreateTypeDef(), CodeProperty.CreateTypeDef()
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
        /// <param name="applicationName">The name of the application.</param>
        /// <param name="applicationFile">The application file as specified by
        /// the user, requesting the application to be started.</param>
        /// <param name="applicationBinaryFile">A compiled version of the
        /// application file.</param>
        /// <param name="applicationHostFile">The path to the binary that are
        /// actually to be loaded (i.e. the assembly).</param>
        /// <param name="workingDirectory">The application working directory.</param>
        /// <param name="entrypointArguments">Arguments to be passed to the
        /// application entrypoint, if one exist.</param>
        /// <param name="execEntryPointSynchronously">Indicates if the entrypoint
        /// should be executed synchrounously, i.e. before this method return.</param>
        /// <param name="stopwatch">An optional stopwatch to use for timing.</param>
        public static unsafe void ExecuteApplication(
            void* hsched,
            string applicationName,
            string applicationFile,
            string applicationBinaryFile,
            string applicationHostFile,
            string workingDirectory,
            string[] entrypointArguments,
            bool execEntryPointSynchronously = false,
            Stopwatch stopwatch = null) {

            var application = new Application(applicationName, applicationFile, applicationBinaryFile, applicationHostFile, workingDirectory, entrypointArguments);

            stopwatch_ = stopwatch ?? Stopwatch.StartNew();

            OnLoaderStarted();

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

            var unregisteredTypeDefs = new List<TypeDef>(typeDefs.Count);
            for (int i = 0; i < typeDefs.Count; i++) {
                var typeDef = typeDefs[i];
                var alreadyRegisteredTypeDef = Bindings.GetTypeDef(typeDef.Name);
                if (alreadyRegisteredTypeDef == null) {
                    unregisteredTypeDefs.Add(typeDef);
                } else {
                    // If the type has a different ASSEMBLY than the already
                    // loaded type, we raise an error. We match by exact version,
                    // i.e including the revision and build.

                    bool assemblyMatch = true;
                    if (!AssemblyName.ReferenceMatchesDefinition(
                        typeDef.TypeLoader.AssemblyName,
                        alreadyRegisteredTypeDef.TypeLoader.AssemblyName)) {
                        assemblyMatch = false;
                    } else if (typeDef.TypeLoader.AssemblyName.Version == null) {
                        assemblyMatch = alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version == null;
                    } else if (alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version == null) {
                        assemblyMatch = false;
                    } else {
                        assemblyMatch = typeDef.TypeLoader.AssemblyName.Version.Equals(
                            alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version);
                    }

                    if (!assemblyMatch) {
                        throw ErrorCode.ToException(
                            Starcounter.Error.SCERRTYPEALREADYLOADED,
                            string.Format("Type failing: {0}. Already loaded: {1}",
                            typeDef.TypeLoader.ScopedName, 
                            alreadyRegisteredTypeDef.TypeLoader.ScopedName));
                    }

                    // A type with the exact matching name has already been loaded
                    // from an assembly with the exact same matching name and the
                    // exact same version. We are still not certain they are completely
                    // equal, but we won't do a full equality-on-value implementation
                    // now. It's for a future release.
                    // TODO:
                    // Provide full checking of type defintion (including table
                    // definition) to see they fully match.
                }
            }

            OnUnregisteredTypeDefsDetermined();

            var assembly = assemblyResolver.ResolveApplication(inputFile.FullName);

            OnTargetAssemblyLoaded();

            Package package = new Package(
                unregisteredTypeDefs.ToArray(),
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
        private static void OnUnregisteredTypeDefsDetermined() { Trace("Unregistered type definitions determined."); }
        private static void OnTargetAssemblyLoaded() { Trace("Target assembly loaded."); }
        private static void OnPackageCreated() { Trace("Package created."); }
        private static void OnPackageProcessed() { Trace("Package processed."); }

        [Conditional("TRACE")]
        private static void Trace(string message)
        {
            Diagnostics.WriteTrace("loader", stopwatch_.ElapsedTicks, message);

            Diagnostics.WriteTimeStamp("LOADER", message);
        }
    }
}
