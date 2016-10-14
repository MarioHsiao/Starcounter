
namespace Starcounter.Bootstrap
{
    public interface IHostConfiguration
    {
        uint ChunksNumber { get; }
        uint DefaultSessionTimeoutMinutes { get; }
        ushort DefaultSystemHttpPort { get; }
        ushort DefaultUserHttpPort { get; }
        bool EnableTraceLogging { get; }
        byte GatewayNumberOfWorkers { get; }
        uint InstanceID { get; }
        string Name { get; }
        bool NoDb { get; }
        bool NoNetworkGateway { get; }
        string OutputDirectory { get; }
        uint SchedulerCount { get; }
        string ServerName { get; }
        ushort SQLProcessPort { get; }
        string TempDirectory { get; }
    }
}