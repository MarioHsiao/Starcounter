using Starcounter.CLI;
using System;

namespace staradmin.Commands {

    internal class StartDatabaseCommand : StartCommand {

        protected override void Start() {
            var cmd = StartDatabaseCLICommand.Create();
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmd.DatabaseName = Context.Database;
            }
            cmd.Execute();
        }
    }
}