
using Starcounter;
using System;
using System.Collections;

namespace Starcounter.Binding
{

    public abstract class PropertyBinding : IPropertyBinding
    {

        internal Int32 _dataIndex;
        internal Int32 _index;
        internal String _name;

#if false // TODO:
        //
        // The type or extension binding the property binding belongs to. Set
        // when the property binding is created.
        //
        internal TypeBinding _belongsTo;
#endif

        public Int32 Index { get { return _index; } }

        public String Name { get { return _name; } }

        public abstract ITypeBinding TypeBinding
        {
            get;
        }

        public abstract DbTypeCode TypeCode
        {
            get;
        }

        /// <summary>
        /// Gets the value of a binary attribute
        /// </summary>
        protected abstract Binary DoGetBinary(object obj);

        /// <summary>
        /// Gets the value of a boolean attribute.
        /// </summary>
        protected abstract Boolean? DoGetBoolean(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits unsigned integer.
        /// </summary>
        protected abstract Byte? DoGetByte(object obj);

        /// <summary>
        /// Gets the value of a timestamp attribute.
        /// </summary>
        protected abstract DateTime? DoGetDateTime(object obj);

        /// <summary>
        /// Gets the value of a decimal attribute.
        /// </summary>
        protected abstract Decimal? DoGetDecimal(object obj);

        /// <summary>
        /// Gets the value of a 64-bits floating point attribute.
        /// </summary>
        protected abstract Double? DoGetDouble(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits signed integer.
        /// </summary>
        protected abstract Int16? DoGetInt16(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits signed integer.
        /// </summary>
        protected abstract Int32? DoGetInt32(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits signed integer.
        /// </summary>
        protected abstract Int64? DoGetInt64(object obj);

        /// <summary>
        /// Gets the value of a reference attribute.
        /// </summary>
        protected abstract Entity DoGetObject(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits signed integer.
        /// </summary>
        protected abstract SByte? DoGetSByte(object obj);

        /// <summary>
        /// Gets the value of a 32-bits floating point attribute.
        /// </summary>
        protected abstract Single? DoGetSingle(object obj);

        /// <summary>
        /// Gets the value of a string attribute.
        /// </summary>
        protected abstract String DoGetString(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits unsigned integer.
        /// </summary>
        protected abstract UInt16? DoGetUInt16(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits unsigned integer.
        /// </summary>
        protected abstract UInt32? DoGetUInt32(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits unsigned integer.
        /// </summary>
        protected abstract UInt64? DoGetUInt64(object obj);

        internal Binary GetBinary(object obj)
        {
            return DoGetBinary(obj);
        }

        internal Boolean? GetBoolean(object obj)
        {
            return DoGetBoolean(obj);
        }

        internal Byte? GetByte(object obj)
        {
            return DoGetByte(obj);
        }

        internal DateTime? GetDateTime(object obj)
        {
            return DoGetDateTime(obj);
        }

        internal Decimal? GetDecimal(object obj)
        {
            return DoGetDecimal(obj);
        }

        internal Double? GetDouble(object obj)
        {
            return DoGetDouble(obj);
        }

        internal Int16? GetInt16(object obj)
        {
            return DoGetInt16(obj);
        }

        internal Int32? GetInt32(object obj)
        {
            return DoGetInt32(obj);
        }

        internal Int64? GetInt64(object obj)
        {
            return DoGetInt64(obj);
        }

        internal Entity GetObject(object obj)
        {
            return DoGetObject(obj);
        }

        internal SByte? GetSByte(object obj)
        {
            return DoGetSByte(obj);
        }

        internal Single? GetSingle(object obj)
        {
            return DoGetSingle(obj);
        }

        internal String GetString(object obj)
        {
            return DoGetString(obj);
        }

        internal UInt16? GetUInt16(object obj)
        {
            return DoGetUInt16(obj);
        }

        internal UInt32? GetUInt32(object obj)
        {
            return DoGetUInt32(obj);
        }

        internal UInt64? GetUInt64(object obj)
        {
            return DoGetUInt64(obj);
        }

        internal Int32 GetDataIndex()
        {
            return _dataIndex;
        }

        internal void SetDataIndex(Int32 dataIndex)
        {
            _dataIndex = dataIndex;
        }

        internal void SetIndex(Int32 index)
        {
            _index = index;
        }

        internal void SetName(String name)
        {
            _name = name;
        }
    }
}
