
using System;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Define behavior of a custom assembly resolver.
    /// </summary>
    public interface IAssemblyResolver
    {
        Assembly ResolveApplicationReference(ResolveEventArgs args);
    }
}