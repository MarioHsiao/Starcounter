
using System;
namespace Starcounter.Advanced {
    
    /// <summary>
    /// </summary>
    public class DbEnvironment {

        /// <summary>
        /// </summary>
        public DbEnvironment(string databaseName, bool hasDatabase) { // TODO: Internal

            if (string.IsNullOrEmpty(databaseName)) throw new ArgumentException("databaseName");

            DatabaseName = databaseName;
            HasDatabase = hasDatabase;
        }

        /// <summary>
        /// Name of the database.
        /// </summary>
        public string DatabaseName { get; private set; }


        /// <summary>
        /// Gets a value indicating whether there is a database attached to the current applet
        /// </summary>
        public bool HasDatabase { get; private set; }
    }
}
