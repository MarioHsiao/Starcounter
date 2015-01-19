

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

        /// <summary>
        /// Creates a <see cref="HostedColumn"/> from values consumed
        /// from a <see cref="ColumnInfo"/>.
        /// </summary>
        /// <param name="column">The column to create the corresponding
        /// hosted column from.</param>
        /// <param name="referencedTargetType">An optional name of a
        /// target type, in case the hosted column should represent a
        /// reference to another type.
        /// </param>
        /// <returns>A new hosted column.</returns>
        public static HostedColumn From(ColumnDef column, string referencedTargetType = null) {
            return new HostedColumn() {
                Name = column.Name,
                TypeCode = BindingHelper.ConvertScTypeCodeToDbTypeCode(column.Type),
                TargetType = referencedTargetType
            };
        }
    }
}