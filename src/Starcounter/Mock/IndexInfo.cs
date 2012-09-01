
using Starcounter;
using Starcounter.Query.Execution;
using Sc.Server.Internal;
using System;

namespace Sc.Server.Binding
{
    internal class IndexInfo : Object
    {
        private UInt64 _handle;
        private String _name;
        //private IndexType _indexType;
        private TypeOrExtensionBinding _typeBinding;
        private PropertyBinding[] _propertyBindings;
        private SortOrder[] _sortOrderings;

        //internal IndexInfo(Int64 handle, IndexType indexType, TypeOrExtensionBinding typeBinding, PropertyBinding propertyBinding)
        //    : base()
        //{
        //    _handle = handle;
        //    _indexType = indexType;
        //    _typeBinding = typeBinding;
        //    _propertyBinding = propertyBinding;
        //}

        // New constructor to support combined indexes.
        internal IndexInfo(UInt64 handle, String name, TypeOrExtensionBinding typeBinding, PropertyBinding[] propertyBindings, SortOrder[] sortOrderings)
        {
            if (propertyBindings.Length != sortOrderings.Length)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible propertyBindings and sortOrderings.");
            }
            _handle = handle;
            _name = name;
            _typeBinding = typeBinding;
            _propertyBindings = propertyBindings;
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

        ///// <summary>
        ///// Type of index.
        ///// </summary>
        //public IndexType IndexType {get {return _indexType;}}

        ///// <summary>
        ///// Indexed path.
        ///// </summary>
        //public String Path {get {return _propertyBinding.Name;}}

        /// <summary>
        /// The number of attributes (paths) in the (combined) index.
        /// </summary>
        public Int32 AttributeCount
        {
            get
            {
                return _propertyBindings.Length;
            }
        }

        /// <summary>
        /// Returns the name of the path with the specified index number within the combined index.
        /// </summary>
        /// <param name="index">An index number within the combined index.</param>
        /// <returns>The name of the path with the input index number.</returns>
        public String GetPathName(Int32 index)
        {
            return _propertyBindings[index].Name;
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
            return _propertyBindings[index].TypeCode;
        }

        /// <summary>
        /// Type binding.
        /// </summary>
        public TypeOrExtensionBinding TypeBinding
        {
            get
            {
                return _typeBinding;
            }
        }

        ///// <summary>
        ///// Value type code. Specifies the type of the indexed path.
        ///// </summary>
        //public DbTypeCode ValueType {get {return _propertyBinding.TypeCode;}}
    }
}
