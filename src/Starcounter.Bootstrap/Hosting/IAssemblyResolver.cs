
using System;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Define behavior of a custom assembly resolver.
    /// </summary>
    internal interface IAssemblyResolver
    {
        Assembly ResolveApplicationReference(ResolveEventArgs args);
    }
}