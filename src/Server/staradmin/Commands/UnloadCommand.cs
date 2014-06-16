
using System;

namespace staradmin.Commands {

    internal class UnloadCommand : ContextAwareCommand {
        class Sources {
            public const string Database = "db";
        }

        readonly string source;

        public UnloadCommand(string dataSource = null) {
            source = string.IsNullOrEmpty(dataSource) ? Sources.Database : dataSource;
        }

        /// <inheritdoc />
        public override void Execute() {
            switch (source.ToLowerInvariant()) {
                case Sources.Database:
                    UnloadDatabase();
                    break;
                default:
                    ReportUnrecognizedSource();
                    break;
            }
        }

        void UnloadDatabase() {
            throw new NotImplementedException();
        }

        void ReportUnrecognizedSource() {
            var helpOnUnload = new ShowHelpCommand(CommandLine.Commands.Unload.Name) { SupressHeader = true };
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to unload '{0}'.", source), helpOnUnload);
            badInput.Execute();
        }
    }
}
