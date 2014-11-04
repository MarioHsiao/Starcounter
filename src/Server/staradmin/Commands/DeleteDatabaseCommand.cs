using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin.Commands {

    internal class DeleteDatabaseCommand : DeleteCommand {
        
        protected override void Delete() {
            // Get the name of the database we are about to
            // delete.
            // If not FORCE, ask the user to confirm by writing
            // the name of the database.
            // Carry out the delete.
            // TODO:

            throw new NotImplementedException();
        }
    }
}