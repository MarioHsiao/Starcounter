

namespace Starcounter.Binding {
    /// <summary>
    /// Represents a column in a table, as it is known to the
    /// runtime host.
    /// </summary>
    public class HostedColumn {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbTypeCode"/> of the hosted
        /// column.
        /// </summary>
        public DbTypeCode TypeCode { get; set; }

        /// <summary>
        /// Gets or sets the name of the type the current hosted
        /// column reference; assumes the type of the colum is
        /// <see cref="DbTypeCode.Object"/>.
        /// </summary>
        public string TargetType { get; set; }
    }
}