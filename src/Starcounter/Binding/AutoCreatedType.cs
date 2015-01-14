
using Starcounter.Hosting;
using Starcounter.Internal;
using System.Collections.Generic;

namespace Starcounter.Binding {

    /// <summary>
    /// Internal class and table keeping track of types instantiated in
    /// user tables by the Starcounter binder when applications are using
    /// dynamic types.
    /// </summary>
    public sealed class AutoCreatedType : SystemEntity {
        #region Infrastructure, reflecting what is emitted by the weaver.
        static bool initialized = false;
        internal static void InitType() {
            if (!initialized) {
                initialized = true;
                HostManager.InitTypeSpecification(typeof(AutoCreatedType.__starcounterTypeSpecification));
            }
        }
#pragma warning disable 0649, 0169
        internal class __starcounterTypeSpecification {
            internal static ushort tableHandle;
            internal static TypeBinding typeBinding;
            internal static int columnHandle_Name;
            internal static int columnHandle_ID;
        }
#pragma warning disable 0628, 0169
        #endregion

        /// <summary>
        /// Creates the <see cref="TypeDef"/> (and underlying <see cref="TableDef"/>)
        /// that represent the current type.
        /// </summary>
        /// <returns>A type definition for the current .NET type.</returns>
        static internal TypeDef CreateTypeDef() {
            var name = typeof(AutoCreatedType).FullName;
            string baseName = null;
            var columns = new List<ColumnDef>();
            var properties = new List<PropertyDef>();
            var hostedColumns = new List<HostedColumn>();

            var column = new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, false);
            columns.Add(column);
            hostedColumns.Add(HostedColumn.From(column));

            column = new ColumnDef("Name", sccoredb.STAR_TYPE_STRING, true, false);
            columns.Add(column);
            hostedColumns.Add(HostedColumn.From(column));
            properties.Add(PropertyDef.FromMappedColumn(column));

            column = new ColumnDef("ID", sccoredb.STAR_TYPE_ULONG, false, false);
            columns.Add(column);
            hostedColumns.Add(HostedColumn.From(column));
            properties.Add(PropertyDef.FromMappedColumn(column));

            var table = new TableDef(name, baseName, columns.ToArray());
            return TypeDef.DefineNew(
                name,
                baseName,
                table,
                TypeLoader.ForStarcounterType(name),
                properties.ToArray(),
                hostedColumns.ToArray()
                );
        }

        /// <inheritdoc />
        public AutoCreatedType(Uninitialized u)
            : base(u) {
        }

        internal AutoCreatedType()
            : this(null) {
            DbState.SystemInsert(__starcounterTypeSpecification.tableHandle, ref this.__sc__this_id__, ref this.__sc__this_handle__);
        }

        /// <summary>
        /// Name of the type we have auto-created a type instance for.
        /// </summary>
        public string Name {
            get { return DbState.ReadString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name); }
            internal set {
                DbState.WriteString(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_Name,
                    value);
            }
        }

        /// <summary>
        /// The object identity of the record created as the type.
        /// </summary>
        public ulong ID {
            get { return DbState.ReadUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_ID); }
            internal set {
                DbState.WriteUInt64(__sc__this_id__, __sc__this_handle__, __starcounterTypeSpecification.columnHandle_ID,
                    value);
            }
        }
    }
}
