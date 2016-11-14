
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.Internal.Weaver.Cache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Starcounter.Weaver.MsBuild
{
    public class Weave : Task
    {
        [Required]
        public string AssemblyFile { get; set; }

        [Required]
        public string CacheDirectory { get; set; }

        public bool DisableCache { get; set; }

        public bool AttachDebugger { get; set; }

        public override bool Execute()
        {
            if (AttachDebugger)
            {
                Debugger.Launch();
            }

            if (!File.Exists(AssemblyFile))
            {
                Log.LogError($"Unable to weave {AssemblyFile}: file does not exist");
                return false;
            }

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }

            WeaverSetup setup;
            var result = WeaveToCache(out setup);
            if (result)
            {
                var weavedArtifacts = CachedAssemblyFiles.GetAllFromCacheDirectoryMatchingSources(CacheDirectory, setup.InputDirectory);
                return CopyWeavedArtifactsToOutputDirectory(weavedArtifacts);
            }

            return result;
        }

        bool WeaveToCache(out WeaverSetup setup)
        {
            // We are weaving to cache only, so output directory should not really
            // be in play here.

            var ignoredOutputDirectory = Path.Combine(CacheDirectory, ".output_ignored");
            if (!Directory.Exists(ignoredOutputDirectory))
            {
                Directory.CreateDirectory(ignoredOutputDirectory);
            }

            // Bug: when doing CLEAN Simplified.Data.Model.dll and Starcounter.Extensions.dll
            // is not deleted from the OUTPUT directory.
            //
            // And second bug: when that happens, and you then build the project, the weaver
            // will fail ScErrAppDeplyedEditionLibrary, but the build report success (and the
            // weaver cache is empty).
            //
            // TODO:

            setup = new WeaverSetup()
            {
                InputDirectory = Path.GetDirectoryName(AssemblyFile),
                AssemblyFile = AssemblyFile,
                OutputDirectory = ignoredOutputDirectory,
                CacheDirectory = CacheDirectory
            };
            setup.WeaveToCacheOnly = true;
            setup.DisableWeaverCache = DisableCache;

            Log.LogMessageFromText($"Weaving {AssemblyFile} -> {CacheDirectory}", MessageImportance.High);

            var weaver = WeaverFactory.CreateWeaver(setup, typeof(MsBuildWeaverHost));
            try
            {
                var result = weaver.Execute();
                Trace.Assert(result);
                return result;
            }
            catch (Exception ex) when (MsBuildWeaverHost.IsOriginatorOf(ex))
            {
                var errorsAndWarnings = MsBuildWeaverHost.DeserializeErrorsAndWarnings(ex);

                foreach (var errorOrWarning in errorsAndWarnings)
                {
                    if (errorOrWarning.IsWarning)
                    {
                        Log.LogWarning(errorOrWarning.Message);
                    }
                    else
                    {
                        var errorCode = ErrorCode.ToDecoratedCode(errorOrWarning.ErrorCode);
                        var helpLink = ErrorCode.ToHelpLink(errorOrWarning.ErrorCode);

                        Log.LogError(null, errorCode, helpLink, string.Empty, 0, 0, 0, 0, errorOrWarning.Message);
                    }
                }

                return !Log.HasLoggedErrors;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
        }

        bool CopyWeavedArtifactsToOutputDirectory(IEnumerable<CachedAssemblyFiles> files)
        {
            var outputDirectory = Path.GetDirectoryName(AssemblyFile);
            
            foreach (var cachedFiles in files)
            {
                cachedFiles.CopyTo(outputDirectory, true, CachedAssemblyArtifact.Binaries);
            }

            return true;
        }
    }
}
