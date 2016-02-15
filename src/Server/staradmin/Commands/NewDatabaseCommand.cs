
using Starcounter.CLI;

namespace staradmin.Commands {

    internal class NewDatabaseCommand : NewCommand {

        protected override void New() {
            var cmd = CreateDatabaseCLICommand.Create();
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmd.DatabaseName = Context.Database;
            }

            var parameters = Context.CommandParameters;
            if (parameters.Count > 1) {
                // First parameter is always the type, i.e. "db", remove that.
                parameters.RemoveAt(0);

                // After that, we support a simple name and key value properties,
                // e.g staradmin new db foo defaulthttpuserport=80
                if (!parameters[0].Contains("=")) {
                    cmd.DatabaseName = parameters[0];
                    parameters.RemoveAt(0);
                }

                if (parameters.Count > 0) {
                    cmd.ParseAndApplyParameters(parameters);
                }
            }

            cmd.Execute();
        }
    }
}