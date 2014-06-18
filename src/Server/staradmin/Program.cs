﻿
using Sc.Tools.Logging;
using staradmin.Commands;
using Starcounter;
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace staradmin {
    using Severity = Sc.Tools.Logging.Severity;

    class Program {
        static void Main(string[] args) {
            CommandLine.PreParse(ref args);
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();
            SharedCLI.InitCLIContext();

            var runNewParser = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAR_ADMIN_NEW_SYNTAX"));
            if (runNewParser) {
                var appArgs = CommandLine.Parse(args);
                
                var factory = new CommandFactory();
                var command = factory.CreateCommand(appArgs);

                command.Execute();
                return;
            }

            try {
                string command = args.Length > 0 ? args[0] : string.Empty;
                command = command.ToLowerInvariant();
                command = command.TrimStart('-', '/');
                switch (command) {
                    case "killall":
                        throw new Exception("No longer supported: use 'staradmin kill all'");
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
                        throw new Exception("No longer supported: use 'STAR_ADMIN_NEW_SYNTAX' and 'staradmin console'");
                    case "log":
                        throw new Exception("No longer supported: use 'staradmin list log'");
                    case "unload":
                        throw new Exception("No longer supported: use 'staradmin unload db'");
                    case "reload":
                        throw new Exception("No longer supported: use 'staradmin reload db'");
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