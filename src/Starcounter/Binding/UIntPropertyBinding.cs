// ***********************************************************************
// <copyright file="UIntPropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class UIntPropertyBinding
    /// </summary>
    public abstract class UIntPropertyBinding : PrimitivePropertyBinding
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="UIntPropertyBinding" /> class.
        /// </summary>
        public UIntPropertyBinding() : base() { }

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
            UInt64? value;
            value = DoGetUInt64(obj);
            return value.HasValue ? new Decimal?(value.Value) : null;
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
        /// Gets the value of a reference attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>Entity.</returns>
        protected override sealed IObjectView DoGetObject(object obj)
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
            UInt64? value;
            value = DoGetUInt64(obj);
            return value.HasValue ? value.Value.ToString() : null;
        }

        /// <summary>
        /// Exceptions the type of for invalid.
        /// </summary>
        /// <returns>Exception.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to access an unsigned attribute as something other then an unsigned attribute.</exception>
        internal Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access an unsigned attribute as something other then an unsigned attribute."
            );
        }
    }
}
