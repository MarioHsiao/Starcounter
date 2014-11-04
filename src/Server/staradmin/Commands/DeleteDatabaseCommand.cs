using Starcounter.CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin.Commands {

    internal class DeleteDatabaseCommand : DeleteCommand {
        
        protected override void Delete() {
            var cli = new AdminCLI(Context.ServerReference);
            var database = Context.Database;

            if (!Force) {
                var granted = GrantRightToDelete(database);
                if (!granted) {
                    Console.WriteLine("Aborting...");
                    return;
                }
            }

            var result = cli.DeleteDatabase(database, true);
            if (result == 0) {
                ConsoleUtil.ToConsoleWithColor(
                    string.Format("Database {0} successfully deleted", database),
                    ConsoleColor.Green
                    );
            }
        }

        bool GrantRightToDelete(string database) {
            Console.WriteLine("You are asking to delete database \"{0}\"", database);
            Console.WriteLine("There is no going back - ALL data will be lost.");
            Console.WriteLine("Are you sure about this?");
            Console.WriteLine();
            Console.WriteLine("If you are sure, enter the name of the database (\"{0}\"), the press ENTER", database);
            Console.WriteLine("To abort, just press ENTER.");

            var granted = false;
            while (!granted) {
                var input = Console.ReadLine();
                input = input.Trim().Trim('"');
                if (input.Equals(database, StringComparison.InvariantCultureIgnoreCase)) {
                    granted = true;
                    break;
                } else if (input == string.Empty) {
                    break;
                } else {
                    Console.WriteLine("Wrong name: {0}. Try again.", input);
                    Console.WriteLine();
                }
            }

            return granted;
        }
    }
}