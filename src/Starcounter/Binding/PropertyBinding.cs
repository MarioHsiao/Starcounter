// ***********************************************************************
// <copyright file="PropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections;
using System.Diagnostics;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class PropertyBinding
    /// </summary>
    public abstract class PropertyBinding : IPropertyBinding
    {

        /// <summary>
        /// Index of column if representing a database column. -1 otherwise.
        /// </summary>
        internal Int32 _dataIndex;
        /// <summary>
        /// The _index
        /// </summary>
        internal Int32 _index;
        /// <summary>
        /// The _name
        /// </summary>
        internal String _name;
        /// <summary>
        /// The _uppername
        /// </summary>
        internal String _lowername;

        /// <summary>
        /// Property index.
        /// </summary>
        /// <value>The index.</value>
        public Int32 Index { get { return _index; } }

        /// <summary>
        /// Property name.
        /// </summary>
        /// <value>The name.</value>
        public String Name { get { return _name; } }

        /// <summary>
        /// Gets property name friendly to display.
        /// </summary>
        public String DisplayName { get { return _name; } }

        /// <summary>
        /// Gets the name of the upper.
        /// </summary>
        /// <value>The name of the upper.</value>
        public String LowerName { get { return _lowername; } }

        /// <summary>
        /// Binding used by the property type if any.
        /// </summary>
        /// <value>The type binding.</value>
        /// <returns>
        /// A type binding. Null if the target is a literal, only set if a
        /// reference property.
        ///   </returns>
        public abstract ITypeBinding TypeBinding
        {
            get;
        }

        /// <summary>
        /// Property value type code.
        /// </summary>
        /// <value>The type code.</value>
        public abstract DbTypeCode TypeCode
        {
            get;
        }

        /// <summary>
        /// Gets the value of a binary attribute
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Binary.</returns>
        protected abstract Binary DoGetBinary(object obj);

        /// <summary>
        /// Gets the value of a boolean attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Boolean}.</returns>
        protected abstract Boolean? DoGetBoolean(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Byte}.</returns>
        protected abstract Byte? DoGetByte(object obj);

        /// <summary>
        /// Gets the value of a timestamp attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        protected abstract DateTime? DoGetDateTime(object obj);

        /// <summary>
        /// Gets the value of a decimal attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Decimal}.</returns>
        protected abstract Decimal? DoGetDecimal(object obj);

        /// <summary>
        /// Gets the value of a 64-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Double}.</returns>
        protected abstract Double? DoGetDouble(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int16}.</returns>
        protected abstract Int16? DoGetInt16(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int32}.</returns>
        protected abstract Int32? DoGetInt32(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int64}.</returns>
        protected abstract Int64? DoGetInt64(object obj);

        /// <summary>
        /// Gets the value of a reference attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Entity.</returns>
        protected abstract IObjectView DoGetObject(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{SByte}.</returns>
        protected abstract SByte? DoGetSByte(object obj);

        /// <summary>
        /// Gets the value of a 32-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Single}.</returns>
        protected abstract Single? DoGetSingle(object obj);

        /// <summary>
        /// Gets the value of a string attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>String.</returns>
        protected abstract String DoGetString(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt16}.</returns>
        protected abstract UInt16? DoGetUInt16(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt32}.</returns>
        protected abstract UInt32? DoGetUInt32(object obj);

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt64}.</returns>
        protected abstract UInt64? DoGetUInt64(object obj);

        /// <summary>
        /// Gets the binary.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Binary.</returns>
        internal Binary GetBinary(object obj)
        {
            return DoGetBinary(obj);
        }

        /// <summary>
        /// Gets the boolean.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Boolean}.</returns>
        internal Boolean? GetBoolean(object obj)
        {
            return DoGetBoolean(obj);
        }

        /// <summary>
        /// Gets the byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Byte}.</returns>
        internal Byte? GetByte(object obj)
        {
            return DoGetByte(obj);
        }

        /// <summary>
        /// Gets the date time.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        internal DateTime? GetDateTime(object obj)
        {
            return DoGetDateTime(obj);
        }

        /// <summary>
        /// Gets the decimal.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Decimal}.</returns>
        internal Decimal? GetDecimal(object obj)
        {
            return DoGetDecimal(obj);
        }

        /// <summary>
        /// Gets the double.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Double}.</returns>
        internal Double? GetDouble(object obj)
        {
            return DoGetDouble(obj);
        }

        /// <summary>
        /// Gets the int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int16}.</returns>
        internal Int16? GetInt16(object obj)
        {
            return DoGetInt16(obj);
        }

        /// <summary>
        /// Gets the int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int32}.</returns>
        internal Int32? GetInt32(object obj)
        {
            return DoGetInt32(obj);
        }

        /// <summary>
        /// Gets the int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int64}.</returns>
        internal Int64? GetInt64(object obj)
        {
            return DoGetInt64(obj);
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Entity.</returns>
        internal IObjectView GetObject(object obj)
        {
            return DoGetObject(obj);
        }

        /// <summary>
        /// Gets the S byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{SByte}.</returns>
        internal SByte? GetSByte(object obj)
        {
            return DoGetSByte(obj);
        }

        /// <summary>
        /// Gets the single.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Single}.</returns>
        internal Single? GetSingle(object obj)
        {
            return DoGetSingle(obj);
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>String.</returns>
        internal String GetString(object obj)
        {
            return DoGetString(obj);
        }

        /// <summary>
        /// Gets the U int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt16}.</returns>
        internal UInt16? GetUInt16(object obj)
        {
            return DoGetUInt16(obj);
        }

        /// <summary>
        /// Gets the U int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt32}.</returns>
        internal UInt32? GetUInt32(object obj)
        {
            return DoGetUInt32(obj);
        }

        /// <summary>
        /// Gets the U int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt64}.</returns>
        internal UInt64? GetUInt64(object obj)
        {
            return DoGetUInt64(obj);
        }

        /// <summary>
        /// Gets the Index of column if representing a database column. -1 otherwise.
        /// </summary>
        /// <returns>the index of column if so</returns>
        internal Int32 GetDataIndex()
        {
            return _dataIndex;
        }

        /// <summary>
        /// Sets the index of the data.
        /// </summary>
        /// <param name="dataIndex">Index of the data.</param>
        internal void SetDataIndex(Int32 dataIndex)
        {
            _dataIndex = dataIndex;
        }

        /// <summary>
        /// Sets the index.
        /// </summary>
        /// <param name="index">The index.</param>
        internal void SetIndex(Int32 index)
        {
            _index = index;
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        /// <param name="name">The name.</param>
        internal void SetName(String name)
        {
            _name = name;
            _lowername = name.ToLower();
        }

#if DEBUG
        /// <summary>
        /// Comparing this and given objects and asserting that they are equal.
        /// </summary>
        /// <param name="other">The given object to compare with this object.</param>
        /// <returns>True if the objects are equals and false otherwise.</returns>
        public bool AssertEquals(IPropertyBinding other) {
            PropertyBinding otherNode = other as PropertyBinding;
            Debug.Assert(otherNode != null);
            return this.AssertEquals(otherNode);
        }
        internal bool AssertEquals(PropertyBinding other) {
            Debug.Assert(other != null);
            if (other == null)
                return false;
            // Check basic types
            Debug.Assert(this._index == other._index);
            if (this._index != other._index)
                return false;
            Debug.Assert(this._dataIndex == other._dataIndex);
            if (this._dataIndex != other._dataIndex)
                return false;
            Debug.Assert(this._name == other._name);
            if (this._name != other._name)
                return false;
            Debug.Assert(this._lowername == other._lowername);
            if (this._lowername != other._lowername)
                return false;
            return true;
        }
#endif
    }
}
