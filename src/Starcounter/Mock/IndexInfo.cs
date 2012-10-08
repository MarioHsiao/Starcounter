
using Starcounter;
using Starcounter.Query.Execution;
using System;

namespace Starcounter.Binding
{

    internal class IndexInfo : Object
    {
        private UInt64 _handle;
        private String _name;
        private ColumnDef[] _columnDefs;
        private SortOrder[] _sortOrderings;

        internal IndexInfo(UInt64 handle, String name, ColumnDef[] columnDefs, SortOrder[] sortOrderings)
        {
#if false
            if (columnDefs.Length != sortOrderings.Length)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible propertyBindings and sortOrderings.");
            }
#endif
            _handle = handle;
            _name = name;
            _columnDefs = columnDefs;
            _sortOrderings = sortOrderings;
        }

        /// <summary>
        /// Index handle. Used to access the index.
        /// </summary>
        public UInt64 Handle
        {
            get
            {
                return _handle;
            }
        }

        /// <summary>
        /// Name of the index. Used by user to identify the index, for example in query hints.
        /// </summary>
        public String Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The number of attributes (paths) in the (combined) index.
        /// </summary>
        public Int32 AttributeCount
        {
            get
            {
                return _columnDefs.Length;
            }
        }

        /// <summary>
        /// Returns the name of the path with the specified index number within the combined index.
        /// </summary>
        /// <param name="index">An index number within the combined index.</param>
        /// <returns>The name of the path with the input index number.</returns>
        public String GetPathName(Int32 index)
        {
            return _columnDefs[index].Name;
        }

        /// <summary>
        /// Returns the sort ordering of the path with the specified index number within the combined index.
        /// </summary>
        /// <param name="index">An index number within the combined index.</param>
        /// <returns>The sort ordering of the path with the input index number.</returns>
        public SortOrder GetSortOrdering(Int32 index)
        {
            return _sortOrderings[index];
        }

        /// <summary>
        /// Returns the database type code of the path with the specified index number within the combined index.
        /// </summary>
        /// <param name="index">An index number within the combined index.</param>
        /// <returns>The type code of the path with the input index number.</returns>
        public DbTypeCode GetTypeCode(Int32 index)
        {
            return _columnDefs[index].Type;
        }
    }
}
