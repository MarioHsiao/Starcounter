
using Starcounter.CLI;

namespace staradmin.Commands {

    internal class StopDbCommand : StopCommand {

        protected override void Stop() {
            var cmd = StopDatabaseCommand.Create();
            cmd.StopCodeHostOnly = false;
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmd.DatabaseName = Context.Database;
            }

            cmd.Execute();
        }
    }
}

