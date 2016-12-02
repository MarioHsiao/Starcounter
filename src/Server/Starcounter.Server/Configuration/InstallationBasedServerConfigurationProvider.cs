using Starcounter.Internal;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Starcounter.Advanced.Configuration
{
    /// <summary>
    /// Provide a <see cref="ServerConfiguration"/> based on a given installation
    /// directory.
    /// </summary>
    public static class InstallationBasedServerConfigurationProvider
    {
        static object monitor = new object();
        static Cached cached = null;

        class Cached
        {
            public string InstallationDirectory { get; set; }
            public ServerConfiguration Value { get; set; }
        }

        public static ServerConfiguration GetConfiguration()
        {
            return GetConfiguration(StarcounterEnvironment.InstallationDirectory);
        }
        
        public static ServerConfiguration GetConfiguration(string installationDirectory)
        {
            Guard.DirectoryExists(installationDirectory, nameof(installationDirectory));

            lock (monitor)
            {
                if (cached != null && DirectoryExtensions.EqualDirectories(cached.InstallationDirectory, installationDirectory))
                {
                    return cached.Value;
                }
                
                var serverConfig = LoadConfigurationFromDisk(installationDirectory);
                cached = new Cached()
                {
                    InstallationDirectory = installationDirectory,
                    Value = serverConfig
                };

                return cached.Value;
            }
        }

        static ServerConfiguration LoadConfigurationFromDisk(string installationDirectory)
        {
            var configDir = Path.Combine(installationDirectory, StarcounterEnvironment.Directories.InstallationConfiguration);
            var configFile = Path.Combine(configDir, StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile);

            var xml = XDocument.Load(configFile);
            var query = from c in xml.Root.Descendants(MixedCodeConstants.ServerConfigDirName)
                        select c.Value;
            var serverDir = query.First();
            var serverConfigPath = Path.Combine(serverDir, StarcounterEnvironment.ServerNames.PersonalServer + ServerConfiguration.FileExtension);

            return ServerConfiguration.Load(serverConfigPath);
        }
    }
}
