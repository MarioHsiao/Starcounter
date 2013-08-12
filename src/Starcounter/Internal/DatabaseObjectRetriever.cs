
using System;
namespace Starcounter.Advanced {

    /// <summary>
    /// Used by the data binding implementation (IBindable) to find a database object using its object number.
    /// </summary>
    public class DatabaseObjectRetriever : IBindableRetriever {

        /// <summary>
        /// The single instance of this object look class
        /// </summary>
        public static DatabaseObjectRetriever Instance = new DatabaseObjectRetriever();

        /// <summary>
        /// Returns the database object with the supplied object number
        /// </summary>
        /// <param name="id">The ObjectNo of the database object</param>
        /// <returns>The database objects</returns>
        public IBindable Retrieve(UInt64 id) {
            return Starcounter.DbHelper.FromID(id);
        }
    }
}
