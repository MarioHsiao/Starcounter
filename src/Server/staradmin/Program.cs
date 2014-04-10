
using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();
            SharedCLI.InitCLIContext();

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
                    case "console":
                        RunConsoleSessionInCurrentProcess(args);
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

        static void RunConsoleSessionInCurrentProcess(string[] args) {
            var consoles = new List<CodeHostConsole>();
            if (args.Length == 1) {
                consoles.Add(new CodeHostConsole(StarcounterConstants.DefaultDatabaseName));
            } else {
                for (int i = 1; i < args.Length; i++) {
                    consoles.Add(new CodeHostConsole(args[i]));
                }
            }

            var session = ConsoleSession.StartNew(consoles.ToArray());
            Console.CancelKeyPress += (s, e) => {
                session.Stop();
            };
            session.Wait();
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