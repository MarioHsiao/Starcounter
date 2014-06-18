using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace staradmin.Commands {
    internal class NewAppCommand : NewCommand {
        readonly string templateName;
        readonly CLITemplate template;
        
        internal NewAppCommand(CLITemplate t) {
            template = t;
        }

        internal NewAppCommand(string nameOfTemplate) {
            templateName = nameOfTemplate;
        }

        protected override void New() {
            var t = template;
            if (t == null) {
                t = CLITemplate.GetTemplate(templateName);
                if (t == null) {
                    var help = ShowHelpCommand.CreateAsInternalHelp(FactoryCommand.Info.Name);
                    var error = new ReportBadInputCommand(
                        ErrorCode.ToMessage(Error.SCERRCLITEMPLATENOTFOUND, string.Format("Name: {0}", templateName)), help);
                    error.Execute();
                    return;
                }
            }

            var app = t.Instantiate();
            ConsoleUtil.ToConsoleWithColor(string.Format("Created {0}", app), ConsoleColor.DarkGray);
            LaunchEditorOnNewAppIfConfigured(app);
        }

        void LaunchEditorOnNewAppIfConfigured(string applicationFile) {
            var editor = Environment.GetEnvironmentVariable("STAR_CLI_APP_EDITOR");
            editor = editor ?? string.Empty;
            switch (editor) {
                case "shell":
                    Process.Start(applicationFile);
                    break;
                case "":
                    break;
                default:
                    Process.Start(editor, applicationFile);
                    break;
            }
        }
    }
}
