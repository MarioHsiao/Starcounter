using System.Collections.Generic;

namespace GenerateMetadataClasses.Model {
#pragma warning disable 0649, 0169
    public class Table {
        public Schema Schema;

        public string TableName;
        public string BaseTableName;
        public bool PublicNamespace;

        public List<Column> Columns;

        /// <summary>
        /// Gets a value indicating if the table specifies an
        /// explicit base table.
        /// </summary>
        public bool HasBaseTable {
            get { return !string.IsNullOrWhiteSpace(BaseTableName); }
        }
    }
#pragma warning restore 0649, 0169
}