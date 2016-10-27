
using Starcounter.CommandLine;
using Starcounter.Hosting;
using Starcounter.Internal;
using StarcounterInternal.Bootstrap;
using System;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Bootstrap
{
    /// <summary>
    /// Host configuration based on a (syntantically correct and parsed)
    /// command-line.
    /// </summary>
    public sealed class CommandLineConfiguration : IHostConfiguration
    {
        #region Provider of IAppStart if auto exec path is given
        class AutoExecStart : IAppStart
        {
            readonly string autoStartPath;
            readonly ApplicationArguments appArgs;

            private AutoExecStart(ApplicationArguments args, string path)
            {
                appArgs = args;
                autoStartPath = path;
            }

            public static IAppStart CreateIfAutoStartIsDefined(ApplicationArguments args)
            {
                string path;
                var defined = args.TryGetProperty(StarcounterConstants.BootstrapOptionNames.AutoStartExePath, out path);
                return defined ? new AutoExecStart(args, path) : null;
            }

            string IAppStart.AssemblyPath {
                get {
                    return autoStartPath;
                }
            }

            string IAppStart.WorkingDirectory {
                get {
                    string workingDir = null;
                    var defined = appArgs.TryGetProperty(StarcounterConstants.BootstrapOptionNames.WorkingDir, out workingDir);
                    return defined ? workingDir : Path.GetDirectoryName(autoStartPath);
                }
            }

            string[] IAppStart.EntrypointArguments {
                get {
                    string userArgs = null;
                    var defined = appArgs.TryGetProperty(StarcounterConstants.BootstrapOptionNames.UserArguments, out userArgs);
                    return defined ? ParseAutoStartingAppUserArguments(userArgs) : null;
                }
            }

            EntrypointOptions IAppStart.EntrypointOptions {
                get {
                    return EntrypointOptions.RunSynchronous;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the program arguments.
        /// </summary>
        /// <value>The program arguments.</value>
        public ApplicationArguments ProgramArguments { get; set; }

        /// <summary>
        /// Gets the scheduler count.
        /// </summary>
        /// <value>The scheduler count.</value>
        public uint SchedulerCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineConfiguration" /> class.
        /// </summary>
        /// <param name="args">Arguments to parse</param>
        public CommandLineConfiguration(string[] args) : this(ParseToArguments(args))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineConfiguration" /> class.
        /// </summary>
        /// <param name="programArguments">The program arguments.</param>
        public CommandLineConfiguration(ApplicationArguments programArguments)
        {
            ProgramArguments = programArguments;

            if (programArguments.ContainsFlag("attachdebugger"))
            {
                Debugger.Launch();
            }

            string prop;
            if (ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.SchedulerCount, out prop))
            {
                try
                {
                    SchedulerCount = uint.Parse(prop);
                }
                catch (Exception e)
                {
                    throw ErrorCode.ToException(Starcounter.Error.SCERRBADSCHEDCOUNTCONFIG, e);
                }
            }
            else
            {
                SchedulerCount = (uint)Environment.ProcessorCount;
            }

            // Checking if there are too many schedulers.
            if (SchedulerCount >= 32) {
                SchedulerCount = 31;
            }
        }

        /// <summary>
        /// Return an <c>IAppStart</c> based on command-line values if specified,
        /// or null otherwise.
        /// </summary>
        /// <returns><c>IAppStart</c> or <c>null</c></returns>
        public IAppStart GetAutoExecStart()
        {
            return AutoExecStart.CreateIfAutoStartIsDefined(ProgramArguments);
        }

        /// <summary>
        /// Gets the database UUID.
        /// </summary>
        /// <value>The database UUID.</value>
        public Guid DbUUID {
            get {
                return Guid.Parse(this.ProgramArguments.CommandParameters[0]);
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get 
            {
                return this.ProgramArguments.CommandParameters[1];
            }
        }

#if false // TODO EOH: Remove these from command-line.
        /// <summary>
        /// Gets the database directory.
        /// </summary>
        /// <value>The database directory.</value>
        public string DatabaseDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.DatabaseDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        /// <summary>
        /// Gets the transaction log directory.
        /// </summary>
        /// <value>The transaction log directory.</value>
        public string TransactionLogDirectory {
            get {
                string dir;

                if (!this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.TransactionLogDirectory, out dir)) {
                    // Fallback on the database directory if the transaction log
                    // directory is not explicitly given.
                    dir = DatabaseDirectory;
                }

                return dir;
            }
        }
#endif

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        /// <value>The output directory.</value>
        public string OutputDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.OutputDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        /// <summary>
        /// Gets the temp directory.
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.TempDir, out prop))
                    prop = @"C:/Test/Temp";

                return prop;
            }
        }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <value>The name of the server.</value>
        public string ServerName
        {
            get
            {
                string serverName;

                if (!this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.ServerName, out serverName))
                    serverName = StarcounterEnvironment.ServerNames.PersonalServer.ToUpper();

                // Making server name upper case.
                serverName = serverName.ToUpper();

                return serverName;
            }
        }

        /// <summary>
        /// Number of workers in gateway.
        /// </summary>
        public Byte GatewayNumberOfWorkers
        {
            get
            {
                if (NoNetworkGateway)
                    return 0;

                String gatewayWorkersNumber;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.GatewayWorkersNumber, out gatewayWorkersNumber))
                {
                    return Byte.Parse(gatewayWorkersNumber);
                }

                throw ErrorCode.ToException(Starcounter.Error.SCERRBADGATEWAYWORKERSNUMBERCONFIG);
            }
        }

        /// <summary>
        /// Default session timeout.
        /// </summary>
        public UInt32 DefaultSessionTimeoutMinutes
        {
            get
            {
                UInt32 defaultSessionsTimeout = StarcounterConstants.NetworkPorts.DefaultSessionTimeoutMinutes;
                String s;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.DefaultSessionTimeoutMinutes, out s))
                {
                    defaultSessionsTimeout = UInt32.Parse(s);
                    if (defaultSessionsTimeout <= 0)
                        throw ErrorCode.ToException(Starcounter.Error.SCERRBADSESSIONSDEFAULTTIMEOUT);
                }

                return defaultSessionsTimeout;
            }
        }

        /// <summary>
        /// Gets the chunks number.
        /// </summary>
        /// <value>The chunks number.</value>
        public uint ChunksNumber
        {
            get
            {
                // Default communication shared chunks number.
                uint chunksNumber = MixedCodeConstants.SHM_CHUNKS_DEFAULT_NUMBER;

                string chunksNumberStr;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.ChunksNumber, out chunksNumberStr))
                {
                    chunksNumber = uint.Parse(chunksNumberStr);

                    // Checking if number of chunks is correct.
                    if ((chunksNumber < 128) || (chunksNumber > 4096 * 512))
                    {
                        throw ErrorCode.ToException(Starcounter.Error.SCERRBADCHUNKSNUMBERCONFIG);
                    }
                }

                return chunksNumber;
            }
        }

        /// <summary>
        /// Gets the SQL process port.
        /// </summary>
        /// <value>The SQL process port.</value>
        public UInt16 SQLProcessPort
        {
            get
            {
                UInt16 v = 0;
                string str;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.SQLProcessPort, out str))
                {
                    v = UInt16.Parse(str);
                }
                return v;
            }
        }

        /// <summary>
        /// Gets the default user HTTP port.
        /// </summary>
        /// <value>The default user HTTP port.</value>
        public UInt16 DefaultUserHttpPort
        {
            get
            {
                UInt16 v = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;
                string str;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.DefaultUserHttpPort, out str))
                {
                    v = UInt16.Parse(str);
                }
                return v;
            }
        }

        /// <summary>
        /// Gets the default user HTTP port.
        /// </summary>
        /// <value>The default user HTTP port.</value>
        public UInt16 DefaultSystemHttpPort
        {
            get
            {
                UInt16 v = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
                string str;
                if (this.ProgramArguments.TryGetProperty(StarcounterConstants.BootstrapOptionNames.DefaultSystemHttpPort, out str))
                {
                    v = UInt16.Parse(str);
                }
                return v;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether [no db].
        /// </summary>
        /// <value><c>true</c> if [no db]; otherwise, <c>false</c>.</value>
        public bool NoDb
        {
            get
            {
                return this.ProgramArguments.ContainsFlag(StarcounterConstants.BootstrapOptionNames.NoDb);
            }
        }

        /// <summary>
        /// Gets a value indicating whether network gateway should not be used.
        /// </summary>
        /// <value><c>true</c> if network gateway should not be used; otherwise, <c>false</c>.</value>
        public bool NoNetworkGateway
        {
            get
            {
                return this.ProgramArguments.ContainsFlag(StarcounterConstants.BootstrapOptionNames.NoNetworkGateway);
            }
        }
        
        public bool EnableTraceLogging {
            get {
                return ProgramArguments.ContainsFlag(StarcounterConstants.BootstrapOptionNames.EnableTraceLogging);
            }
        }
        
        /// <summary>
        /// Simple parser for user arguments.
        /// </summary>
        static string[] ParseAutoStartingAppUserArguments(string userArgs)
        {
            var parmChars = userArgs.ToCharArray();
            var inQuote = false;

            for (int i = 0; i < parmChars.Length; i++)
            {
                if (parmChars[i] == '"')
                {
                    parmChars[i] = '\n';
                    inQuote = !inQuote;
                }

                if (!inQuote && parmChars[i] == ' ')
                    parmChars[i] = '\n';
            }

            return (new string(parmChars)).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
        
        static ApplicationArguments ParseToArguments(string[] args)
        {
            ApplicationArguments arguments;
            ProgramCommandLine.TryGetProgramArguments(args, out arguments);
            return arguments;
        }

    }
}