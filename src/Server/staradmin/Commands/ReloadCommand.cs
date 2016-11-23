
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal class ReloadCommand : ContextAwareCommand {
        /// <summary>
        /// Govern metadata provision about this class as a user command
        /// and defines the factory method to create the actual command
        /// to be executed.
        /// </summary>
        public class UserCommand : IUserCommand {
            readonly CommandLine.Command reload = new CommandLine.Command() {
                Name = "reload",
                ShortText = "Reloads data into a data source, usually a database",
                Usage = "staradmin reload [--file=path] [source] [arguments]"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var syntax = appSyntax.DefineCommand(reload.Name, reload.ShortText);
                syntax.MinParameterCount = 0;
                syntax.MaxParameterCount = 2;
                syntax.DefineProperty("file", "Specifies the file to reload from");
                return syntax;
            }

            public CommandLine.Command Info {
                get { return reload; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                var source = args.CommandParameters.Count == 0 ? string.Empty : args.CommandParameters[0];
                return new ReloadCommand(this, source);
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
                rows.Add("db", "Reloads a database. If no source is given, 'db' is used as the default.");
                table.Write(rows);

                table.Title = "Examples:";
                table.RowSpace = 1;
                rows.Clear();
                rows.Add("staradmin reload db", "Reloads the default reload file into the default database.");
                rows.Add("staradmin --d=foo reload db", "Reloads the default reload file into the 'foo' database");
                rows.Add("staradmin --d=bar reload db --file=data.sql", "Reloads the 'data.sql' file, resolved to the same directory from which the command runs, into the 'bar' database.");
                rows.Add("staradmin reload", "Shorthand for 'staradmin reload db'");
                writer.WriteLine();
                table.Write(rows);
            }
        }

        class Sources {
            public const string Database = "db";
        }

        readonly UserCommand userCommand;
        readonly string source;

        ReloadCommand(UserCommand cmd, string dataSource = null) {
            userCommand = cmd;
            source = string.IsNullOrEmpty(dataSource) ? Sources.Database : dataSource;
        }

        /// <inheritdoc />
        public override void Execute() {
            switch (source.ToLowerInvariant()) {
                case Sources.Database:
                    ReloadDatabase();
                    break;
                default:
                    ReportUnrecognizedSource();
                    break;
            }
        }

        void ReloadDatabase() {
            var reloadSourceCodeFile = "ReloadDatabase.cs";
            string exeFile;

            var appFile = Path.Combine(
                Context.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                reloadSourceCodeFile
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, null, out exeFile);

            var reload = StartApplicationCLICommand.FromFile(appFile, exeFile, CreateReloadApplicationArguments());
            reload.JobDescription = string.Format("Reloading {0}", reload.DatabaseName);
            reload.JobCompletionDescription = "done";
            reload.ApplicationStartingDescription = "reloading";

            reload.Execute();
        }

        ApplicationArguments CreateReloadApplicationArguments() {
            var appName = "Starcounter_Internal_Reload_Utility";

            var syntaxDef = new ApplicationSyntaxDefinition();
            syntaxDef.ProgramDescription = "staradmin.exe Reload";
            syntaxDef.DefaultCommand = "reload";
            SharedCLI.DefineWellKnownOptions(syntaxDef, true);
            syntaxDef.DefineCommand("reload", "Reloads a database");
            var syntax = syntaxDef.CreateSyntax();

            var cmdLine = new List<string>();
            cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.AppName, appName));
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.Db, Context.Database));
            }

            string file;
            if (Context.TryGetCommandProperty("file", out file)) {
                file = Path.GetFullPath(file);
                cmdLine.Add(string.Format("{0}", file));
            }

            ApplicationArguments args;
            SharedCLI.TryParse(cmdLine.ToArray(), syntax, out args);
            return args;
        }

        void ReportUnrecognizedSource() {
            var helpOnReload = ShowHelpCommand.CreateAsInternalHelp(userCommand.Info.Name);
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to reload '{0}'.", source), helpOnReload);
            badInput.Execute();
        }
    }
}
