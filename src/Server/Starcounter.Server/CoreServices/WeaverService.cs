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

            try {
                ToolInvocationHelper.InvokeTool(new ProcessStartInfo(weaverExe, arguments));
            } catch (ToolInvocationException e) {
                ConvertAndRaiseExceptionFromFailingWeaver(e);
            }

            return Path.Combine(runtimeDirectory, Path.GetFileName(givenAssembly));
        }

        public static void ExtractParcelledErrors(string[] content, string parcelID, List<string> errors, int maxCount = -1) {
            string currentParcel;

            currentParcel = null;

            foreach (var inputString in content) {
                if (inputString == null)
                    continue;

                // Are we currently parsing a multi-line parcel?

                if (currentParcel != null) {
                    // Yes we are.
                    // Check if we have reached the final line.

                    if (inputString.EndsWith(parcelID)) {
                        // End the parcel.

                        currentParcel += " " + inputString.Substring(0, inputString.Length - parcelID.Length);
                        errors.Add(currentParcel);
                        if (errors.Count == maxCount)
                            return;

                        currentParcel = null;
                    } else {
                        // Append the current line to the already
                        // identified parcel content and continue.

                        currentParcel += " " + inputString;
                    }
                } else {
                    // We are currently not in the middle of parsing a
                    // parcel. Check the input.

                    if (inputString.StartsWith(parcelID)) {
                        // Beginning of a new parcel. Create it.

                        currentParcel = inputString.Substring(parcelID.Length);

                        // Check if it's a one-line parcel and if it is,
                        // terminate it.

                        if (inputString.EndsWith(parcelID)) {
                            currentParcel = currentParcel.Substring(0, currentParcel.Length - parcelID.Length);
                            errors.Add(currentParcel);
                            if (errors.Count == maxCount)
                                return;

                            currentParcel = null;
                        }
                    }
                }
            }
        }

        void ConvertAndRaiseExceptionFromFailingWeaver(ToolInvocationException e) {
            var errors = new List<string>(1);
            ExtractParcelledErrors(e.Result.GetErrorOutput(), WeaverErrorParcelId, errors, 1);
            Trace.Assert(errors.Count == 1);

            throw ErrorMessage.Parse(errors[0]).ToException();
        }
    }
}
