
using Starcounter;
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands
{
    internal class TestCommand : ContextAwareCommand
    {
        public class UserCommand : IUserCommand
        {
            readonly CommandLine.Command console = new CommandLine.Command()
            {
                Name = "test",
                ShortText = "Run unit tests in a Starcounter database",
                Usage = "staradmin test <testassembly>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax)
            {
                var syntax = appSyntax.DefineCommand(console.Name, console.ShortText);
                syntax.MinParameterCount = 1;
                return syntax;
            }

            public CommandLine.Command Info {
                get { return console; }
            }

            public ICommand CreateCommand(ApplicationArguments args)
            {
                return new TestCommand(this, args.CommandParameters[0]);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer)
            {
                if (!helpCommand.SupressHeader)
                {
                    writer.WriteLine(Info.ShortText);
                    writer.WriteLine();
                }
                writer.WriteLine("Usage: {0}", Info.Usage);

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Examples:";
                table.RowSpace = 1;
                rows.Add("staradmin test tests.dll", "Run tests in `tests.dll`, in the default database");
                rows.Add("staradmin --d=foo test tests.dll", "Run tests in `tests.dll`, in the `foo` database");
                writer.WriteLine();
                table.Write(rows);
            }
        }

        readonly string testAssembly;

        
        TestCommand(UserCommand cmd, string assembly)
        {
            assembly = Path.GetFullPath(assembly);
            testAssembly = assembly;
        }

        public override void Execute()
        {
            var request = new TestRequest();
            request.Assemblies = new[] { testAssembly };

            var node = Context.ServerReference.CreateNode();
            var uri = $"/sc/test/{Context.Database.ToLowerInvariant()}";

            var startDb = StartDatabaseCLICommand.Create();
            startDb.DatabaseName = Context.Database;
            startDb.Execute();

            var response = node.POST(uri, request.ToBytes(), null);
            response.FailIfNotSuccessOr(503);
            if (response.StatusCode == 503)
            {
                SharedCLI.ShowInformationAndSetExitCode(
                    "Unable to run tests (server not running)",
                    Error.SCERRSERVERNOTRUNNING,
                    showStandardHints: false,
                    color: ConsoleColor.DarkGray
                );
                return;
            }

            ReportResults(request, TestResult.FromBytes(response.BodyBytes));
        }

        void ReportResults(TestRequest request, TestResult result)
        {
            Console.WriteLine("Assemblies run:");
            foreach (var a in request.Assemblies)
            {
                Console.WriteLine($"{a}");
            }
            Console.WriteLine();

            Console.WriteLine("Summary:");
            Console.WriteLine($"Tests run: {result.TotalTestsRun}, Failures: {result.TestsFailed}, Run time: 0.000s");
            Console.WriteLine();
        }
    }
}
