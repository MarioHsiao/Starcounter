
using System;

namespace Starcounter.Internal.Weaver.Cache
{
    [Flags]
    public enum CachedAssemblyArtifact
    {
        Schema = 1,
        Assembly = 2,
        Symbols = 4,
        Binaries = Assembly | Symbols,
        All = Schema | Binaries
    }
}