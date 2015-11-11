
using Starcounter.Internal;

namespace Starcounter.Metadata {
    /// <summary>
    /// Defines a base class used by all generated metadata classes.
    /// </summary>
    public abstract class MetadataEntity : SystemEntity {
        /// <inheritdoc />
        protected MetadataEntity(Uninitialized u) : base(u) {}

        /// <summary>
        /// Defines the constructor that instantiate all metadata types
        /// in the database.
        /// </summary>
        /// <param name="tableHandle">The table handle representing the type to
        /// insert.</param>
        protected MetadataEntity(ushort tableHandle) : this(null) {
            DbState.SystemInsert(tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }
    }
}