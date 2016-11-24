
using Starcounter.CLI;

namespace star {

    static class StarOption {
        public const string Help = "help";
        public const string HelpEx = "helpextended";
        public const string HelpUnofficial = "helpunofficial";
        public const string Version = "version";
        public const string Info = "info";
        public const string Serverport = "serverport";
        public const string Server = SharedCLI.Option.Server;
        public const string ServerHost = SharedCLI.Option.ServerHost;
        public const string Db = SharedCLI.Option.Db;
        public const string AppName = SharedCLI.Option.AppName;
        public const string ResourceDirectory = SharedCLI.Option.ResourceDirectory;
        public const string LogSteps = SharedCLI.Option.LogSteps;
        public const string NoDb = SharedCLI.Option.NoDb;
        public const string NoRestart = SharedCLI.Option.NoRestart;
        public const string Stop = SharedCLI.Option.Stop;
        public const string NoAutoCreateDb = SharedCLI.Option.NoAutoCreateDb;
        public const string Verbose = SharedCLI.Option.Verbose;
        public const string Detailed = SharedCLI.Option.Detailed;
        public const string Logs = SharedCLI.Option.Logs;
        public const string Async = SharedCLI.Option.Async;
        public const string TransactMain = SharedCLI.Option.TransactMain;
        public const string Syntax = "syntax";
        public const string NoColor = "nocolor";
        public const string CompileOnly = "sc-compile";
        public const string AdditionalCompilerReferences = "sc-compilerefs";
    }
}