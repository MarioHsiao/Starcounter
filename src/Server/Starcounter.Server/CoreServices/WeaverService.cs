// ***********************************************************************
// <copyright file="WeaverService.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Starcounter.Internal;
using System.Security.Cryptography;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates the services provided by the Starcounter weaver.
    /// </summary>
    internal sealed class WeaverService {
        const string WeaverErrorParcelId = "A4A7D6FA-EB34-442A-B579-DBB1DBB859E3";
        readonly ServerEngine engine;

        /// <summary>
        /// Initializes a <see cref="WeaverService"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> under which
        /// the weaver will run.</param>
        internal WeaverService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of <see cref="WeaverService"/>.
        /// </summary>
        internal void Setup() {
            // Do static initialization, like checking we can find
            // binaries, project files, etc.
            // TODO:

            // Keep a server-global cache with assemblies that we can
            // utilize when we don't find a particular one in the runtime
            // directory given when weaving.
            // TODO:
        }

        /// <summary>
        /// Generates the full directory path where the server should run the
        /// application represented by the assembly <paramref name="assemblyPath"/>
        /// from.
        /// </summary>
        /// <param name="baseDirectory">Full path of the directory the server use
        /// as the base directory when weaving code.</param>
        /// <param name="assemblyPath">Full path to an assembly that are to be
        /// prepared for the host, i.e. about to be weaved.</param>
        /// <returns>A unique subpath of <paramref name="baseDirectory"/> that
        /// should be used by the server to prepare and run the application
        /// represented by the assembly <paramref name="assemblyPath"/>.</returns>
        /// <example>
        /// var dir = weaver.CreateFullRuntimePath(
        ///     @"C:\Server\Code",
        ///     @"C:\My Projects\My First Application\MyApp.exe"
        /// );
        /// 
        /// // The below line will output
        /// //   "C:\Server\Code\myapp.exe-6D189D915AE238F5FA029AE0FBFD0F744113FC2E"
        /// // where the hexadecimal number uniquely identifies the full path specified
        /// // as the second parameter.
        /// Console.WriteLine(dir);
        /// </example>
        internal string CreateFullRuntimePath(string baseDirectory, string assemblyPath) {
            var key = engine.ExecutableService.CreateKey(assemblyPath);
            return Path.Combine(baseDirectory, key);
        }

        /// <summary>
        /// Weaves an assembly and all it's references.
        /// </summary>
        /// <param name="givenAssembly">The path to the original assembly file,
        /// normally corresponding to the path of a starting App executable.
        /// </param>
        /// <param name="runtimeDirectory">The runtime directory to where the
        /// weaved result should be stored. This directory can possibly include
        /// cached (and up-to-date) assemblies weaved from previous rounds.
        /// </param>
        /// <returns>The full path to the corresponding, weaved assembly.</returns>
        internal string Weave(string givenAssembly, string runtimeDirectory) {
            string weaverExe;
            string arguments;

            weaverExe = Path.Combine(engine.InstallationDirectory, StarcounterConstants.ProgramNames.ScWeaver + ".exe");
            arguments = string.Format(
                "--maxerrors=1 --ErrorParcelId={0} Weave \"{1}\" --outdir=\"{2}\"", 
                WeaverErrorParcelId, givenAssembly, runtimeDirectory);
            
            ToolInvocationHelper.InvokeTool(new ProcessStartInfo(weaverExe, arguments));

            return Path.Combine(runtimeDirectory, Path.GetFileName(givenAssembly));
        }
    }
}
