
using Starcounter.Bootstrap;
using System;
using System.IO;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Semantic entrypoint for independent applications to build and
    /// bootstrap self-hosted code hosts.
    /// </summary>
    /// <example>
    /// // The idea:
    /// <code>
    /// var appHost = AppHostBuilder
    ///   UseDatabase("default"). // or ...
    ///   UseDatabaseFileDirectory(""). // and ...
    ///   UseConfiguration(DatabaseConfiguration). // and ...
    ///   UseAppHostSettings(new AppHostSettings() { WrapJSON, LoadExtensionLibraries, etc }). // and ...
    ///   UseApplication("path/to/exe"). // or ..
    ///   UseApplication(Assembly.Current). // or ...
    ///   UseCurrentAssemblyAsApplication().	// This should be the default
    ///   Build();
    ///   
    ///   appHost.Run(() => { Console.Write("Press ENTER to exit"); Console.ReadLine(); });
    /// </code>
    /// When Run(Action) is invoked, the delegate can do the stuff a standard hosted
    /// application can do, like Application.Current.CodeHost, etc. Plus what standard
    /// console/desktop applications can.
    /// </example>
    public class AppHostBuilder
    {
        Func<IHostConfiguration> configProvider;
        Func<IAppStart> appStartProvider;

        public AppHostBuilder UseDatabase(string name)
        {
            var p = new HostConfigurationFromServerConfiguration(name);
            configProvider = p.CreateConfiguration;
            return this;
        }

        public AppHostBuilder UseDatabase(DirectoryInfo databaseDir)
        {
            // Return a builder based on a directory with database
            // files.
            // var p = new DirectoryBasedConfigurationProvider(databaseDir);
            // configProvider = p.CreateConfiguration;
            // TODO:
            throw new NotImplementedException();
        }

        /// <summary>
        /// Specifies the application to bootstrap into the host, given its assembly.
        /// </summary>
        /// <param name="application">Assembly that contains the application.</param>
        /// <param name="entrypointOptions">How the host should consider the entrypoint of
        /// the application.</param>
        /// <returns>An <c>AppHostBuilder</c> including the specified change and that can
        /// be configured further</returns>
        public AppHostBuilder UseApplication(Assembly application, EntrypointOptions entrypointOptions)
        {
            var start = AppStart.FromAssembly(application);
            start.EntrypointOptions = entrypointOptions;
            appStartProvider = () => { return start; };
            return this;
        }

        /// <summary>
        /// Specifies the application to bootstrap into the host, given a path to the assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the application assembly.</param>
        /// <param name="entrypointOptions">How the host should consider the entrypoint of
        /// the application.</param>
        /// <returns>An <c>AppHostBuilder</c> including the specified change and that can
        /// be configured further</returns>
        public AppHostBuilder UseApplication(string assemblyPath, EntrypointOptions entrypointOptions)
        {
            var start = AppStart.FromExecutable(assemblyPath);
            appStartProvider = () => { return start; };
            return this;
        }

        /// <summary>
        /// Specifies that the entrypoint assembly of the currently running application
        /// should be used as the application to host.
        /// </summary>
        /// <remarks>
        /// This is a shorthand version of <c>UseApplication(Assembly.GetEntryAssembly(),
        /// EntrypointOptions.DontRun).</c>
        /// </remarks>
        /// <returns>An <c>AppHostBuilder</c> including the specified change and that can
        /// be configured further</returns>
        public AppHostBuilder UseCurrentApplication()
        {
            return UseApplication(Assembly.GetEntryAssembly(), EntrypointOptions.DontRun);
        }
        
        public ICodeHost Build()
        {
            return new SelfHostedCodeHost(CreateHostConfiguration(), CreateAppStart());
        }

        IHostConfiguration CreateHostConfiguration()
        {
            var config = configProvider?.Invoke();
            if (config == null)
            {
                throw new InvalidOperationException("Unable to build host without a specified database. See .UseDatabase() on how to specify one.");
            }
            return config;
        }

        IAppStart CreateAppStart()
        {
            var appstart = appStartProvider?.Invoke();
            if (appstart == null)
            {
                throw new InvalidOperationException("Unable to build host without a specified application. See .UseApplication() on how to specify one.");
            }
            return appstart;
        }
    }
}