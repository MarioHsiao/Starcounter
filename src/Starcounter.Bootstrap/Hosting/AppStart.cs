
using Starcounter.Bootstrap;
using Starcounter.Bootstrap.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Simple implementation of <c>IAppStart</c> sugared with a few
    /// convenience factory methods for common cases.
    /// </summary>
    internal class AppStart : IAppStart
    {
        public static AppStart FromExecutable(string appAssemblyPath, string[] args = null)
        {
            if (string.IsNullOrEmpty(appAssemblyPath))
            {
                throw new ArgumentNullException(nameof(appAssemblyPath));
            }

            var start = new AppStart()
            {
                AssemblyPath = appAssemblyPath,
                EntrypointArguments = args,
                WorkingDirectory = Path.GetDirectoryName(appAssemblyPath),
                EntrypointOptions = EntrypointOptions.RunSynchronous
            };

            return start;
        }

        public static AppStart FromLibrary(string libAssemblyPath, string[] args = null)
        {
            if (string.IsNullOrEmpty(libAssemblyPath))
            {
                throw new ArgumentNullException(nameof(libAssemblyPath));
            }

            var start = new AppStart()
            {
                AssemblyPath = libAssemblyPath,
                EntrypointArguments = args,
                WorkingDirectory = Path.GetDirectoryName(libAssemblyPath),
                EntrypointOptions = EntrypointOptions.DontRun
            };

            return start;
        }

        public static AppStart FromAssembly(Assembly assembly, string[] args = null)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var location = assembly.Location;
            var mainOptions = assembly.EntryPoint != null
                ? EntrypointOptions.RunSynchronous
                : EntrypointOptions.DontRun;

            var start = new AppStart()
            {
                AssemblyPath = location,
                EntrypointArguments = args,
                WorkingDirectory = Path.GetDirectoryName(location),
                EntrypointOptions = mainOptions
            };

            return start;
        }

        public string AssemblyPath { get; set; }

        public string[] EntrypointArguments { get; set; }

        public EntrypointOptions EntrypointOptions { get; set; }

        public string WorkingDirectory { get; set; }
    }
}
