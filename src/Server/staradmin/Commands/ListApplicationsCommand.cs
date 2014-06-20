using Starcounter.CLI;

namespace staradmin.Commands {

    internal class ListApplicationsCommand : ListCommand {
        protected override void List() {
            AdminCLI.ListApplications();
        }
    }
}