
using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            try {
                string command = args.Length > 0 ? args[0] : string.Empty;
                command = command.ToLowerInvariant();
                command = command.TrimStart('-', '/');
                switch (command) {
                    case "killall":
                        ProcessUtilities.KillAllScProcesses();
                        break;
                    case "installservice":
                        bool start = args.Length > 1 && args[1] == "start";
                        ServerServiceUtilities.Install(start);
                        break;
                    case "uninstallservice":
                        ServerServiceUtilities.Uninstall();
                        break;
                    case "startservice":
                        ServerServiceUtilities.Start();
                        break;
                    case "stopservice":
                        ServerServiceUtilities.Stop();
                        break;
                    default:
                        var template = CLITemplate.GetTemplate(command);
                        if (template == null) {
                            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
                        }
                        var name = args.Length > 1 ? args[1] : null;
                        var path = template.Instantiate(name);
                        ConsoleUtil.ToConsoleWithColor(string.Format("Created {0}", path), ConsoleColor.DarkGray);
                        LaunchEditorOnNewAppIfConfigured(path);
                        break;
                }

            } catch(Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            } finally {
                Console.ResetColor();
            }
        }

        static void LaunchEditorOnNewAppIfConfigured(string applicationFile) {
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