
using Starcounter.CLI;
using System;

namespace staradmin.Commands {

    internal class StartServerCommand : StartCommand {

        protected override void Start() {
            var cmd = StartServerCLICommand.Create();
            cmd.Execute();
        }
    }
}