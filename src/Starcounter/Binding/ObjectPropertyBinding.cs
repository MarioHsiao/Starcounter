// ***********************************************************************
// <copyright file="ObjectPropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class ObjectPropertyBinding
    /// </summary>
    public abstract class ObjectPropertyBinding : PropertyBinding
    {

        /// <summary>
        /// The target type binding_
        /// </summary>
        private TypeBinding targetTypeBinding_;
        /// <summary>
        /// The target type name_
        /// </summary>
        private string targetTypeName_;

        /// <summary>
        /// Gets the type code.
        /// </summary>
        /// <value>The type code.</value>
        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Object; } }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <value>The type binding.</value>
        public override sealed ITypeBinding TypeBinding
        {
            get
            {
                TypeBinding tb;
                tb = targetTypeBinding_;
                if (tb != null) return tb;
                return LookupTargetTypeBinding();
            }
        }

        /// <summary>
        /// Gets the value of a binary attribute
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Binary.</returns>
        protected override sealed Binary DoGetBinary(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a boolean attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Boolean}.</returns>
        protected override sealed Boolean? DoGetBoolean(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Byte}.</returns>
        protected override sealed Byte? DoGetByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a timestamp attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        protected override sealed DateTime? DoGetDateTime(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a decimal attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Decimal}.</returns>
        protected override sealed Decimal? DoGetDecimal(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a 64-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Double}.</returns>
        protected override sealed Double? DoGetDouble(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int16}.</returns>
        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int32}.</returns>
        protected override sealed Int32? DoGetInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Int64}.</returns>
        protected override sealed Int64? DoGetInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 8-bits signed integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{SByte}.</returns>
        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a 32-bits floating point attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{Single}.</returns>
        protected override sealed Single? DoGetSingle(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of a string attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>String.</returns>
        protected override sealed String DoGetString(object obj)
        {
            Object value;
            value = DoGetObject(obj);
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 16-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt16}.</returns>
        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 32-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt32}.</returns>
        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Gets the value of an integer attribute as a 64-bits unsigned integer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Nullable{UInt64}.</returns>
        protected override sealed UInt64? DoGetUInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        /// <summary>
        /// Sets the name of the target type.
        /// </summary>
        /// <param name="targetTypeName">Name of the target type.</param>
        internal void SetTargetTypeName(string targetTypeName)
        {
            targetTypeName_ = targetTypeName;
        }

        /// <summary>
        /// Exceptions the type of for invalid.
        /// </summary>
        /// <returns>Exception.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to access a reference attribute as something other then a reference attribute.</exception>
        private Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access a reference attribute as something other then a reference attribute."
            );
        }

        /// <summary>
        /// Lookups the target type binding.
        /// </summary>
        /// <returns>TypeBinding.</returns>
        private TypeBinding LookupTargetTypeBinding()
        {
            // Thread-safe because it doesn't matter since the method is
            // idempotent. Field targetTypeBinding_ is only a cache.

            TypeBinding tb = Bindings.GetTypeBinding(targetTypeName_);
            targetTypeBinding_ = tb;
            return tb;
        }
    }
}
