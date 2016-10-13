
using Starcounter.Bootstrap;
using System;
using System.IO;

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

        protected virtual IHostConfiguration CreateConfiguration()
        {
            return configProvider();
        }

        public ICodeHost Build()
        {
            var config = CreateConfiguration();
            return new SelfHostedCodeHost(config); 
        }
    }
}