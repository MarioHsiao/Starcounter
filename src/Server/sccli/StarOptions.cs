
using Starcounter.CLI;

namespace star {

    static class StarOption {
        public const string Help = "help";
        public const string HelpEx = "helpextended";
        public const string Version = "version";
        public const string Info = "info";
        public const string Serverport = "serverport";
        public const string Server = SharedCLI.Option.Server;
        public const string ServerHost = SharedCLI.Option.ServerHost;
        public const string Db = SharedCLI.Option.Db;
        public const string LogSteps = SharedCLI.Option.LogSteps;
        public const string NoDb = SharedCLI.Option.NoDb;
        public const string NoAutoCreateDb = SharedCLI.Option.NoAutoCreateDb;
        public const string WaitForEntrypoint = "wait";
        public const string Verbosity = "verbosity";
        public const string Syntax = "syntax";
        public const string NoColor = "nocolor";
        public const string AttatchCodeHostDebugger = "debug";
    }
}