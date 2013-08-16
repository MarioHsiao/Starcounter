
using System;
namespace Starcounter.Advanced {

    /// <summary>
    /// Used by the data binding implementation (IBindable) to find a database object using its object number.
    /// </summary>
    public class DatabaseObjectRetriever : IBindableRetriever {
        private DatabaseObjectRetriever() {
        }

        /// <summary>
        /// The single instance of this object look class
        /// </summary>
        public static readonly DatabaseObjectRetriever Instance = new DatabaseObjectRetriever();

        /// <summary>
        /// Returns the database object with the supplied object number
        /// </summary>
        /// <param name="id">The ObjectNo of the database object</param>
        /// <returns>The database objects</returns>
        public IBindable Retrieve(UInt64 id) {
            return Starcounter.DbHelper.FromID(id);
        }

        /// <summary>
        /// Gets a value that indicates if <paramref name="obj"/> is
        /// considered equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            // This implementation is based on the fact that we allow only a
            // single instance of this class to ever be created. We could
            // compare for is-equality among the types too, but I think it's
            // slower.
            return obj != null && object.ReferenceEquals(this, obj);
        }

        /// <summary>
        /// Computes the hash code of the current instance.
        /// </summary>
        /// <returns>A hashcode representing the current instance.</returns>
        public override int GetHashCode() {
            // Since we are preventing instantiation and forcing a
            // single instance, the hash code for this type can be a constant,
            // whichever.
            return 1;
        }
    }
}
