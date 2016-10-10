
using System;

namespace Starcounter.Bootstrap
{
    public sealed class HostConfiguration : IHostConfiguration
    {
        public string AutoStartExePath { get; set; }

        public string AutoStartUserArguments { get; set; }

        public string AutoStartWorkingDirectory { get; set; }

        public uint ChunksNumber { get; set; }

        public uint DefaultSessionTimeoutMinutes { get; set; }

        public ushort DefaultSystemHttpPort { get; set; }

        public ushort DefaultUserHttpPort { get; set; }

        public bool EnableTraceLogging { get; set; }

        public byte GatewayNumberOfWorkers { get; set; }

        public uint InstanceID { get; set; }

        public string Name { get; set; }

        public bool NoDb { get; set; }

        public bool NoNetworkGateway { get; set; }

        public string OutputDirectory { get; set; }

        public uint SchedulerCount { get; set; }

        public string ServerName { get; set; }

        public ushort SQLProcessPort { get; set; }

        public string TempDirectory { get; set; }
    }
}
