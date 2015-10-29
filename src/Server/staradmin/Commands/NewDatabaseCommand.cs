
using Starcounter.CLI;

namespace staradmin.Commands {

    internal class NewDatabaseCommand : NewCommand {

        protected override void New() {
            var cmd = CreateDatabaseCLICommand.Create();
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmd.DatabaseName = Context.Database;
            }
            cmd.Execute();
        }
    }
}