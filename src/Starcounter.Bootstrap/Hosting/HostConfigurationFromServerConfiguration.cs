
using Starcounter.Bootstrap;
using Starcounter.Internal;
using Starcounter.Server.Rest.Representations.JSON;
using System;

namespace Starcounter.Hosting
{
    using Server = Starcounter.Server.Rest.Representations.JSON.Server;

    /// <summary>
    /// Host configuration provider that leverage configuration from an
    /// admin server based API (and it's underlying configuration).
    /// </summary>
    internal class HostConfigurationFromServerConfiguration
    {
        readonly string DatabaseName;
        readonly int AdminPort;

        public HostConfigurationFromServerConfiguration(string name) : this(name, GetResolvedSystemPort())
        {
        }

        public HostConfigurationFromServerConfiguration(string name, int systemPort)
        {
            DatabaseName = name;
            AdminPort = systemPort;
        }
        
        public IHostConfiguration CreateConfiguration()
        {
            var endpoint = $"127.0.0.1:{AdminPort}";

            var server = GET<Server>(endpoint, "api/server");
            var database = GET<Database>(endpoint, $"api/databases/{DatabaseName}/config");

            return CreateConfiguration(server, database);
        }

        static IHostConfiguration CreateConfiguration(Server server, Database database)
        {
            var config = new HostConfiguration();
            config.ServerName = "PERSONAL";
            config.Name = database.Name.ToUpper();
            config.DbUUID = Guid.Parse(database.DbUUID);
            config.OutputDirectory = server.Logs.LogDirectory;
            config.TempDirectory = database.Configuration.TempDirectory;
            config.DefaultSystemHttpPort = (ushort)server.Configuration.SystemHttpPort;
            config.DefaultUserHttpPort = (ushort)database.Configuration.DefaultUserHttpPort;
            config.SchedulerCount = (uint)database.Configuration.SchedulerCount;
            config.NoDb = false;
            config.NoNetworkGateway = true;     // Until we can fix bug https://github.com/Starcounter/Starcounter/issues/3869#issuecomment-257071378

            return config;
        }

        static T GET<T>(string endpoint, string uri) where T : Json, new()
        {
            var fullUri = $"http://{endpoint}/{uri}";

            var response = Http.GET(fullUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{response.StatusCode}-{response.StatusDescription}: {response.Body}");
            }

            var json = new T();
            json.PopulateFromJson(response.Body);
            return json;
        }

        static int GetResolvedSystemPort()
        {
            return EnvironmentExtensions.GetEnvironmentInteger(
                StarcounterEnvironment.VariableNames.DefaultServerPersonalPort,
                StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort
                );
        }
    }
}
