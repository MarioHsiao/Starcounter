
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
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
                return weaver.Execute();
            }
            catch (Exception ex) when (MsBuildWeaverHost.IsOriginatorOf(ex))
            {
                foreach (var error in MsBuildWeaverHost.EnumerateErrorsFrom(ex))
                {
                    Log.LogError(error);
                }

                // Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}
