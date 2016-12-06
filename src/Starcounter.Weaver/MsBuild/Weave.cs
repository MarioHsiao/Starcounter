
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.Internal;
using Starcounter.Internal.Weaver.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

            var weaver = new InPlaceWeaver(AssemblyFile, CacheDirectory);
            weaver.Setup.DisableWeaverCache = DisableCache;

            bool weaverSuccess = false;
            Log.LogMessageFromText($"Weaving {AssemblyFile} -> {CacheDirectory}", MessageImportance.High);
            try
            {
                var result = weaver.Weave(typeof(MsBuildWeaverHost));
                Trace.Assert(result);
                weaverSuccess = true;
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

                weaverSuccess = !Log.HasLoggedErrors;
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                weaverSuccess = false;
            }

            if (weaverSuccess)
            {
                weaver.CopyWeavedArtifactsToTargetDirectory();
            }

            return weaverSuccess;
        }
    }
}