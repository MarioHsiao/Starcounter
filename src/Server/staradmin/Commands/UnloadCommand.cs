
using Sc.Tools.Logging;
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal class UnloadCommand : ContextAwareCommand {
        /// <summary>
        /// Govern metadata provision about this class as a user command
        /// and defines the factory method to create the actual command
        /// to be executed.
        /// </summary>
        public class UserCommand : IUserCommand {
            readonly CommandLine.Command unload = new CommandLine.Command() {
                Name = "unload",
                ShortText = "Unloads data from a data source, usually a database",
                Usage = "staradmin unload [--file=path] [source] [arguments]"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var syntax = appSyntax.DefineCommand(unload.Name, unload.ShortText);
                syntax.MinParameterCount = 0;
                syntax.MaxParameterCount = 4;
                syntax.DefineProperty("file", "Specifies the file to unload into");
                syntax.DefineFlag("allowPartial", "If specified, allows the unload to be partial");
                syntax.DefineProperty("shiftKey", "Specifies a number that can be used to specifiy object key ranges");
                return syntax;
            }

            public CommandLine.Command Info {
                get { return unload; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                var source = args.CommandParameters.Count == 0 ? string.Empty : args.CommandParameters[0];
                return new UnloadCommand(this, source);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer) {
                if (!helpCommand.SupressHeader) {
                    writer.WriteLine(Info.ShortText);
                    writer.WriteLine();
                }
                writer.WriteLine("Usage: {0}", Info.Usage);

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Sources:";
                rows.Add("db", "Unloads a database. If no source is given, 'db' is used as the default.");
                table.Write(rows);

                table.Title = "Examples:";
                table.RowSpace = 1;
                rows.Clear();
                rows.Add("staradmin unload db", "Unloads the default database into the default unload file.");
                rows.Add("staradmin --d=foo unload db", "Unloads the 'foo' database into the default unload file.");
                rows.Add("staradmin --d=bar unload db --file=data.sql", "Unloads the 'bar' database into the 'data.sql' file, resolved to the same directory from which the command runs.");
                rows.Add("staradmin unload", "Shorthand for 'staradmin unload db'");
                rows.Add("staradmin unload db --allowPartial", "Unloads the default database, allowing the unload to be partial");
                rows.Add("staradmin unload db --shiftKey=99999", "Makes every key being unloaded increase with the given number, i.e. 99999");
                writer.WriteLine();
                table.Write(rows);
            }
        }

        class Sources {
            public const string Database = "db";
        }

        readonly UserCommand userCommand;
        readonly string source;

        UnloadCommand(UserCommand cmd, string dataSource = null) {
            userCommand = cmd;
            source = string.IsNullOrEmpty(dataSource) ? Sources.Database : dataSource;
        }

        /// <inheritdoc />
        public override void Execute() {
            switch (source.ToLowerInvariant()) {
                case Sources.Database:
                    UnloadDatabase();
                    break;
                default:
                    ReportUnrecognizedSource();
                    break;
            }
        }

        void UnloadDatabase() {
            var unloadSourceCodeFile = "UnloadDatabase.cs";
            string exeFile;

            var appFile = Path.Combine(
                Context.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                unloadSourceCodeFile
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, null, null, out exeFile);

            var unload = StartApplicationCLICommand.FromFile(appFile, exeFile, CreateUnloadApplicationArguments());
            unload.JobDescription = string.Format("Unloading {0}", unload.DatabaseName);
            unload.JobCompletionDescription = "done";
            unload.ApplicationStartingDescription = "unloading";

            unload.Execute();
            DisplayWarnings(unload.StartTime); 
        }

        ApplicationArguments CreateUnloadApplicationArguments() {
            var appName = "Starcounter_Internal_Unload_Utility";

            var syntaxDef = new ApplicationSyntaxDefinition();
            syntaxDef.ProgramDescription = "staradmin.exe Unload";
            syntaxDef.DefaultCommand = "unload";
            SharedCLI.DefineWellKnownOptions(syntaxDef, true);
            syntaxDef.DefineCommand("unload", "Unloads a database");
            var syntax = syntaxDef.CreateSyntax();

            var cmdLine = new List<string>();
            cmdLine.Add(string.Format("--{0}", SharedCLI.Option.NoAutoCreateDb));
            cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.AppName, appName));
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.Db, Context.Database));
            }

            var file = "";
            if (Context.TryGetCommandProperty("file", out file)) {
                file = Path.GetFullPath(file);
            }
            else
            {
                // Instruct application to use the default.
                file = "@";
            }
            cmdLine.Add(string.Format("{0}", file));

            var allowPartial = Context.ContainsCommandFlag("allowPartial");
            cmdLine.Add(allowPartial.ToString());

            string shiftKey;
            if (!Context.TryGetCommandProperty("shiftKey", out shiftKey)) {
                shiftKey = "0";
            }
            cmdLine.Add(shiftKey.ToString());
            
            ApplicationArguments args;
            SharedCLI.TryParse(cmdLine.ToArray(), syntax, out args);
            return args;
        }

        void DisplayWarnings(DateTime since) {
            var log = new FilterableLogReader() {
                Count = int.MaxValue,
                Since = since,
                TypeOfLogs = Severity.Warning,
                Source = "Starcounter.Host.Unload"
            };
            var logs = LogSnapshot.Take(log).All;

            var console = new LogConsole();
            foreach (var entry in logs) {
                console.Write(entry);
            }
        }

        void ReportUnrecognizedSource() {
            var helpOnUnload = ShowHelpCommand.CreateAsInternalHelp(userCommand.Info.Name);
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to unload '{0}'.", source), helpOnUnload);
            badInput.Execute();
        }
    }
}
