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
using Starcounter.Logging;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates the services provided by the Starcounter weaver.
    /// </summary>
    internal sealed class WeaverService {
        const string WeaverErrorParcelId = "A4A7D6FA-EB34-442A-B579-DBB1DBB859E3";
        readonly ServerEngine engine;
        LogSource log;
        LogSource weaverLog;

        /// <summary>
        /// Initializes a <see cref="WeaverService"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> under which
        /// the weaver will run.</param>
        internal WeaverService(ServerEngine engine) {
            this.engine = engine;
            this.log = ServerLogSources.Default;
            this.weaverLog = ServerLogSources.Weaver;
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
            bool retriedWithoutCache;

            retriedWithoutCache = false;
            weaverExe = Path.Combine(engine.InstallationDirectory, StarcounterConstants.ProgramNames.ScWeaver + ".exe");
            arguments = CreateWeaverCommandLine(givenAssembly, runtimeDirectory, true);

            runweaver:
            try {
                ToolInvocationHelper.InvokeTool(new ProcessStartInfo(weaverExe, arguments), true);
            } catch (ToolInvocationException e) {
                if (ShouldTryWeaveWithoutCache(e.ExitCode) && !retriedWithoutCache) {
                    // If we detect that the weaver can not weave because of a problem with
                    // the cache, we retry once without using any cached code.
                    // We log this as a notice, since we should eventually try figuring out
                    // a better way to solve this.
                    log.LogNotice("Weaving {0} failed with code {1}. Retrying without the cache.", givenAssembly, e.ExitCode);
                    retriedWithoutCache = true;
                    arguments = CreateWeaverCommandLine(givenAssembly, runtimeDirectory, false);
                    goto runweaver;
                }

                LogAndRaiseExceptionFromFailingWeaver(e);
            }

            return Path.Combine(runtimeDirectory, Path.GetFileName(givenAssembly));
        }

        internal static bool ShouldTryWeaveWithoutCache(int error) {
            return error == Error.SCERRWEAVERCANTUSECACHE || error == Error.SCERRUNHANDLEDWEAVEREXCEPTION;
        }

        string CreateWeaverCommandLine(string givenAssembly, string outputDirectory, bool useCache) {
            var arguments = string.Format(
                "--maxerrors=1 --ErrorParcelId={0} Weave \"{1}\" --outdir=\"{2}\"",
                WeaverErrorParcelId, givenAssembly, outputDirectory);

            if (!useCache) {
                arguments = "--nocache " + arguments;
            }
            
            return arguments;
        }

        void LogAndRaiseExceptionFromFailingWeaver(ToolInvocationException e) {
            var errors = new List<string>(1);

            // When the weaver fails, we always log an error that includes the
            // process exit code at the very minimum. We try to include more, but
            // we never let it stop us if additional information is not available.

            string detail = string.Empty;
            try {
                detail += string.Format("{0}Arguments={1}{0}", Environment.NewLine, e.Result.Arguments);
                detail += "Output:";
                detail += Environment.NewLine;
                foreach (var line in e.Result.GetOutput()) {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    detail += line;
                    detail += Environment.NewLine;
                }
            }
            catch {} 
            finally {
                var msg = ErrorCode.ToMessage(Error.SCERRWEAVINGERROR, string.Format("Process exit code: {0}.{1}", e.ExitCode, detail));
                log.LogError(msg);
            }

            // We then try to format a proper, more pinpointed error from the parcelled
            // weaver error. If failing this, we use the general weaver error as the final
            // fallback.

            Exception result = null;
            try {
                ParcelledError.ExtractParcelledErrors(e.Result.GetErrorOutput(), WeaverErrorParcelId, errors, 1);
                if (errors.Count == 1) {
                    var err = ErrorMessage.Parse(errors[0]);
                    weaverLog.LogError(err.ToString());
                    result = err.ToException();
                }
            } catch (Exception extractFailed) {
                result = ErrorCode.ToException(Error.SCERRWEAVINGERROR, extractFailed, string.Format("Process exit code: {0}", e.ExitCode));
            } finally {
                if (result == null) {
                    result = ErrorCode.ToException(Error.SCERRWEAVINGERROR, string.Format("Process exit code: {0}", e.ExitCode));
                }
            }

            throw result;
        }
    }
}
