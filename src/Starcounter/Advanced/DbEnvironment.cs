
namespace Starcounter.Advanced {
    
    /// <summary>
    /// </summary>
    public class DbEnvironment {

        /// <summary>
        /// </summary>
        public DbEnvironment(string databaseName) { // TODO: Internal
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Name of the database.
        /// </summary>
        public string DatabaseName { get; private set; }
    }
}
