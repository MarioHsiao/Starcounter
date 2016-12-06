using Starcounter.Internal;
using Starcounter.Internal.Weaver.Cache;
using System;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Special-purpose weaver that weaves an application and replace
    /// the ingoing exe with the weaved result.
    /// </summary>
    public class InPlaceWeaver
    {
        WeaverSetup setupFreezed;

        public WeaverSetup Setup { get; private set; }

        public InPlaceWeaver(string assembly, string cacheDirectory)
        {
            Guard.FileExists(assembly, nameof(assembly));
            Guard.DirectoryExists(cacheDirectory, nameof(cacheDirectory));

            // We are weaving to cache only, so output directory should not really
            // be in play here.

            var ignoredOutputDirectory = Path.Combine(cacheDirectory, ".output_ignored");
            if (!Directory.Exists(ignoredOutputDirectory))
            {
                Directory.CreateDirectory(ignoredOutputDirectory);
            }

            Setup = new WeaverSetup()
            {
                AssemblyFile = assembly,
                InputDirectory = Path.GetDirectoryName(assembly),
                CacheDirectory = cacheDirectory,
                OutputDirectory = ignoredOutputDirectory
            };
        }

        public bool Weave(IWeaverHost host)
        {
            Setup.WeaveToCacheOnly = true;
            var weaver = WeaverFactory.CreateWeaver(Setup, host);
            return WeaveToCache(weaver);
        }

        public bool Weave(Type hostType)
        {
            Setup.WeaveToCacheOnly = true;
            var weaver = WeaverFactory.CreateWeaver(Setup, hostType);
            return WeaveToCache(weaver);
        }

        bool WeaveToCache(IWeaver weaver)
        {
            setupFreezed = weaver.Setup;
            return weaver.Execute();
        }

        public void CopyWeavedArtifactsToTargetDirectory()
        {
            var weavedArtifacts = CachedAssemblyFiles.GetAllFromCacheDirectoryMatchingSources(
                setupFreezed.CacheDirectory,
                setupFreezed.InputDirectory
            );

            CopyWeavedArtifactsToOutputDirectory(weavedArtifacts);
        }

        void CopyWeavedArtifactsToOutputDirectory(IEnumerable<CachedAssemblyFiles> files)
        {
            var outputDirectory = Path.GetDirectoryName(setupFreezed.AssemblyFile);

            foreach (var cachedFiles in files)
            {
                cachedFiles.CopyTo(outputDirectory, true, CachedAssemblyArtifact.Binaries);
            }
        }
    }
}
