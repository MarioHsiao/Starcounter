using Starcounter.CLI;

namespace staradmin.Commands {

    internal class StopCodeHostCommand : StopCommand {

        protected override void Stop() {
            var cmd = StopDatabaseCommand.Create();
            cmd.StopCodeHostOnly = true;
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmd.DatabaseName = Context.Database;
            }

            cmd.Execute();
        }
    }
}
