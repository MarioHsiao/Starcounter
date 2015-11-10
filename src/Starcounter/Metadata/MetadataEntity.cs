using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal;
using Starcounter.Binding;

namespace Starcounter.Metadata {
    /// <summary>
    /// Defines a base class used by all generated metadata classes.
    /// By having it a normal database class / table, we can do queries
    /// on all objects relating to metadata.
    /// </summary>
    public abstract class MetadataEntity : SystemEntity {
        #region Binding/hosting specific code
        internal static TypeDef CreateTypeDef() {
            return TypeDef.CreateTypeTableDef(typeof(MetadataEntity));
        }
        internal class @__starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding TypeBinding;
        }
        #endregion

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