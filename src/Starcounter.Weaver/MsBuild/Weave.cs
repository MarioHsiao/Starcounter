﻿
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
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

        public override bool Execute()
        {
            if (!File.Exists(AssemblyFile))
            {
                Log.LogError($"Unable to weave {AssemblyFile}: file does not exist");
                return false;
            }

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }

            // We are weaving to cache only, so output directory should not really
            // be in play here.

            var ignoredOutputDirectory = Path.Combine(CacheDirectory, ".output_ignored");
            if (!Directory.Exists(ignoredOutputDirectory))
            {
                Directory.CreateDirectory(ignoredOutputDirectory);
            }
            
            var setup = new WeaverSetup()
            {
                InputDirectory = Path.GetDirectoryName(AssemblyFile),
                AssemblyFile = AssemblyFile,
                OutputDirectory = ignoredOutputDirectory,
                CacheDirectory = CacheDirectory
            };
            setup.WeaveToCacheOnly = true;

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
        }
    }
}
