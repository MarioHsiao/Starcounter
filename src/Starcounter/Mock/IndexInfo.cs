// ***********************************************************************
// <copyright file="IndexInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;

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

#if DEBUG
        private bool AssertEqualsVisited = false;
        internal bool AssertEquals(IndexInfo other) {
            Debug.Assert(other != null);
            if (other == null)
                return false;
            // Check if there are not cyclic references
            Debug.Assert(!this.AssertEqualsVisited);
            if (this.AssertEqualsVisited)
                return false;
            Debug.Assert(!other.AssertEqualsVisited);
            if (other.AssertEqualsVisited)
                return false;
            // Check basic types
            Debug.Assert(this._handle == other._handle);
            if (this._handle != other._handle)
                return false;
            Debug.Assert(this._name == other._name);
            if (this._name != other._name)
                return false;
            Debug.Assert(this._handle == other._handle);
            if (this._handle != other._handle)
                return false;
            // Check cardinalities of collections
            Debug.Assert(this._columnDefs.Length == other._columnDefs.Length);
            if (this._columnDefs.Length != other._columnDefs.Length)
                return false;
            // Check basic collections
            Debug.Assert(this._sortOrderings.Length == other._sortOrderings.Length);
            if (this._sortOrderings.Length != other._sortOrderings.Length)
                return false;
            for (int i = 0; i < this._sortOrderings.Length; i++) {
                Debug.Assert(this._sortOrderings[i] == other._sortOrderings[i]);
                if (this._sortOrderings[i] != other._sortOrderings[i])
                    return false;
            }
            // Check references. This should be checked if there is cyclic reference.
            AssertEqualsVisited = true;
            bool areEquals = true;
            // Check collections of objects
            for (int i = 0; i < this._columnDefs.Length && areEquals; i++)
                if (this._columnDefs[i] == null) {
                    Debug.Assert(other._columnDefs[i] == null);
                    areEquals = other._columnDefs[i] == null;
                } else {
                    Debug.Assert(this._columnDefs[i] == other._columnDefs[i]);
                    areEquals = this._columnDefs[i] == other._columnDefs[i];
                }
            AssertEqualsVisited = false;
            return areEquals;
        }
#endif
    }
}
