﻿
using Sc.Tools.Logging;
using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace staradmin {
    using Severity = Sc.Tools.Logging.Severity;

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
                    case "log":
                        ViewLogEntries(args);
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

        static void ViewLogEntries(string[] args) {
            // staradmin log <num-entries> <type: debug, notice, warning (default), error>> <source-filter>
            int count = 25;
            var types = Severity.Notice;
            
            if (args.Length > 1) {
                try {
                    count = int.Parse(args[1]);

                    if (args.Length > 2) {
                        var t = args[2].ToLowerInvariant();
                        switch (t) {
                            case "d":
                            case "debug":
                                types = Severity.Debug;
                                break;
                            case "n":
                            case "notice":
                            case "info":
                                types = Severity.Notice;
                                break;
                            case "w":
                            case "warning":
                            case "warnings":
                                types = Severity.Warning;
                                break;
                            case "e":
                            case "error":
                            case "errors":
                                types = Severity.Error;
                                break;
                            default:
                                throw new Exception(string.Format("Unknown log entry type: {0}", t));
                        };
                    }

                } catch (Exception e) {
                    ConsoleUtil.ToConsoleWithColor(string.Format("Invalid command-line: {0}", e.Message), ConsoleColor.Red);
                    return;
                }
            }

            try {
                var console = new LogConsole();
                var reader = new FilterableLogReader() {
                    Count = count,
                    TypeOfLogs = types
                };
                reader.Fetch((log) => { console.Write(log); });
            } catch (Exception e) {
                ConsoleUtil.ToConsoleWithColor(string.Format("Failed getting logs: {0}", e.Message), ConsoleColor.Red);
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