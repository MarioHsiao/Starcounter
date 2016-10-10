
using Starcounter.Bootstrap;
using System;
using System.IO;
using System.Reflection;

namespace Starcounter.Hosting
{
    /*
     * Ideas:
     * 
var appHost =
AppHostBuilder
  UseDatabase("default").
  UseDatabaseFileDirectory("").
  UseConfiguration(DatabaseConfiguration).
  UseAppHostSettings(new AppHostSettings() { WrapJSON, LoadExtensionLibraries, etc).
  UseApplication("path/to/exe").
  UseApplication(Assembly.Current).
  UseCurrentAssemblyAsApplication().	// This should be the default
  Build();

appHost.Run(Action invokeThisAndShutDownProcessWhenItReturn);

When the delegate is invoked, it can do the stuff a standard hosted
application can do, like Application.Current.CodeHost, etc.


IAppHost : ICodeHost
or 
IAppHost {
  ICodeHost { get; }
}

Just have ICodeHost and support Run on that?
And if you do that on the default implementation (the one used
by the shared code host) it's either ignored or you get an exception.
And likewise, if you do that on a process that is already implemented,
you will get an error too.
     */
    public class AppHostBuilder
    {
        public AppHostBuilder UseDatabase(string name)
        {
            // Return a builder based on a configured, named database.
            // TODO:
            throw new NotImplementedException();
        }

        public AppHostBuilder UseDatabase(DirectoryInfo databaseDir)
        {
            // Return a builder based on a directory with database
            // files.
            // TODO:
            throw new NotImplementedException();
        }

        public ICodeHost Build()
        {
            // Prototyping
            // TODO:

            var config = new HostConfiguration();
            config.InstanceID = 1;
            config.Name = "DEFAULT";
            config.ServerName = "PERSONAL";
            config.OutputDirectory = @"C:\Users\Per\Documents\Starcounter\Personal\Logs";
            config.TempDirectory = @"C:\Users\Per\Documents\Starcounter\Personal\Temp";
            config.DefaultSystemHttpPort = 8181;
            config.DefaultUserHttpPort = 8080;
            config.SchedulerCount = (uint)Environment.ProcessorCount;
            config.GatewayNumberOfWorkers = 2;
            config.ChunksNumber = Starcounter.Internal.MixedCodeConstants.SHM_CHUNKS_DEFAULT_NUMBER;

            config.AutoStartExePath = Assembly.GetEntryAssembly().Location;
            config.AutoStartWorkingDirectory = Environment.CurrentDirectory;
            
            return new SelfHostedCodeHost(config); 
        }
    }
}