
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

        public AppHostBuilder UseApplication(Assembly application)
        {
            var start = AppStart.FromAssembly(application);
            appStartProvider = () => { return start; };
            return this;
        }
        
        public ICodeHost Build()
        {
            var config = CreateConfiguration();
            return new SelfHostedCodeHost(config, appStartProvider?.Invoke());
        }

        IHostConfiguration CreateConfiguration()
        {
            return configProvider();
        }
    }
}