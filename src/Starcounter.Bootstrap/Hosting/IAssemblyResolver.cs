
using System;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Define behavior of a custom assembly resolver.
    /// </summary>
    public interface IAssemblyResolver
    {
        Assembly RegisterApplication(string executablePath, ApplicationDirectory appDirectory);
        Assembly ResolveApplicationReference(ResolveEventArgs args);
    }
}