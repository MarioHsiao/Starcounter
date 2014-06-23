using Starcounter.CLI;

namespace staradmin.Commands {

    internal class ListApplicationsCommand : ListCommand {
        protected override void List() {
            var cli = new AdminCLI(ServerReference.CreateDefault());
            cli.ListApplications();
        }
    }
}