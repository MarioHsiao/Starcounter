// ***********************************************************************
// <copyright file="RealPropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class RealPropertyBinding
    /// </summary>
    public abstract class RealPropertyBinding : PrimitivePropertyBinding
    {

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
        /// Gets the value of a string attribute.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>String.</returns>
        protected override sealed String DoGetString(object obj)
        {
            Double? value;
            value = DoGetDouble(obj);
            return value.HasValue ? value.Value.ToString() : null;
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
        /// Exceptions the type of for invalid.
        /// </summary>
        /// <returns>Exception.</returns>
        /// <exception cref="System.NotSupportedException">Attempt to access a real attribute as something other then a real attribute.</exception>
        private Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access a real attribute as something other then a real attribute."
            );
        }
    }
}
