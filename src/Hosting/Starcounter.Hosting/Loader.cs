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

namespace StarcounterInternal.Hosting
{

    /// <summary>
    /// Class BinBriefcase
    /// </summary>
    internal class BinBriefcase
    {

        /// <summary>
        /// The assembly file infos by name_
        /// </summary>
        private Dictionary<string, FileInfo> assemblyFileInfosByName_ = new Dictionary<string, FileInfo>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Adds from directory.
        /// </summary>
        /// <param name="inputDir">The input dir.</param>
        internal void AddFromDirectory(DirectoryInfo inputDir)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            fileInfos.AddRange(inputDir.GetFiles("*.exe"));
            fileInfos.AddRange(inputDir.GetFiles("*.dll"));
            for (int i = 0; i < fileInfos.Count; i++)
            {
                var fileInfo = fileInfos[i];
                var fileName = fileInfo.Name;
                FileInfo previouslyAddedFileInfo;
                if (!assemblyFileInfosByName_.TryGetValue(fileName, out previouslyAddedFileInfo))
                {
                    assemblyFileInfosByName_.Add(fileName, fileInfo);
                }
                else
                {
                    // TODO: Make sure that the files are the same. Checksum?
                }
            }
        }

        /// <summary>
        /// Gets the assembly file.
        /// </summary>
        /// <param name="assemblyFileName">Name of the assembly file.</param>
        /// <returns>FileInfo.</returns>
        internal FileInfo GetAssemblyFile(string assemblyFileName)
        {
            FileInfo ret;
            assemblyFileInfosByName_.TryGetValue(assemblyFileName, out ret);
            return ret;
        }
    }

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

        /// <summary>
        /// The private bin briefcase_
        /// </summary>
        private static readonly BinBriefcase privateBinBriefcase_ = new BinBriefcase();

        private static Stopwatch stopwatch_;

        /// <summary>
        /// Resolves the assembly.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ResolveEventArgs" /> instance containing the event data.</param>
        /// <returns>Assembly.</returns>
        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;

            var assemblyName = args.Name;
            var assemblyNameElems = assemblyName.Split(',');
            var assemblyFileName = string.Concat(assemblyNameElems[0], ".dll");
            var assemblyFileInfo = privateBinBriefcase_.GetAssemblyFile(assemblyFileName);
            if (assemblyFileInfo == null)
            {
                assemblyFileName = string.Concat(assemblyNameElems[0], ".exe");
                assemblyFileInfo = privateBinBriefcase_.GetAssemblyFile(assemblyFileName);
            }

            if (assemblyFileInfo != null)
            {
                assembly = Assembly.LoadFile(assemblyFileInfo.FullName);
            }

            return assembly;
        }

        /// <summary>
        /// Adds the base package.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        public static unsafe void AddBasePackage(void* hsched)
        {
            stopwatch_ = Stopwatch.StartNew();

            var sysTableTypeDef = SysTable.CreateTypeDef();
            var sysColumnTypeDef = SysColumn.CreateTypeDef();
            var sysIndexTypeDef = SysIndex.CreateTypeDef();

            Package package = new Package(
                new TypeDef[] { sysTableTypeDef, sysColumnTypeDef, sysIndexTypeDef },
                null,
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

            // User-level classes are self registring and report in to
            // the installed host manager on first use (via an emitted call
            // in the static class constructor). For system classes, we
            // have to do this by hand.

            HostManager.InitTypeSpecification(typeof(SysTable.__starcounterTypeSpecification));

            stopwatch_ = null;
        }

        /// <summary>
        /// Execs the app.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="workingDirectory">The logical working directory the assembly
        /// will execute in.</param>
        /// <param name="entrypointArguments">Arguments to be passed to the assembly
        /// entrypoint, if any.</param>
        /// <exception cref="StarcounterInternal.Hosting.LoaderException"></exception>
        public static unsafe void ExecApp(
            void* hsched,
            string filePath,
            string workingDirectory = null,
            string[] entrypointArguments = null)
        {
            stopwatch_ = Stopwatch.StartNew();

            try
            {
                filePath = filePath.Trim('\"', '\\');
                filePath = Path.GetFullPath(filePath);
            }
            catch (ArgumentException pathEx)
            {
                throw new LoaderException(string.Format("{0} ({1})", pathEx.Message, filePath));
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new LoaderException(string.Format("File not found: {0}.", filePath));
            }

            var inputFile = new FileInfo(filePath);

            // TODO: Handle duplicates.

            privateBinBriefcase_.AddFromDirectory(inputFile.Directory);

            OnInputVerifiedAndAssemblyResolverUpdated();

            var typeDefs = SchemaLoader.LoadAndConvertSchema(inputFile.Directory);

            OnSchemaVerifiedAndLoaded();

            var unregisteredTypeDefs = new List<TypeDef>(typeDefs.Count);
            for (int i = 0; i < typeDefs.Count; i++)
            {
                var typeDef = typeDefs[i];
                var alreadyRegisteredTypeDef = Bindings.GetTypeDef(typeDef.Name);
                if (alreadyRegisteredTypeDef == null)
                {
                    unregisteredTypeDefs.Add(typeDef);
                }
                else
                {
                    // TODO:
                    // Assure that the already loaded type definition has
                    // the same structure.
                }
            }

            OnUnregisteredTypeDefsDetermined();

            var assembly = Assembly.LoadFile(inputFile.FullName);

            OnTargetAssemblyLoaded();

            Package package = new Package(unregisteredTypeDefs.ToArray(), assembly, stopwatch_);
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                package.WorkingDirectory = workingDirectory;
            }
            package.EntrypointArguments = entrypointArguments;

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
        }
    }
}
